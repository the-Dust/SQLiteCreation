using SQLiteCreation.Context;
using SQLiteCreation.Parsers.Base;
using SQLiteCreation.Repositories.Base;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SQLiteCreation.Repositories
{
    class Repository : AbstractRepository, IRepository
    {
        public event Action<object, string> OnEvent = (object o, string s) => { };
        public event Action<object, string> OnError = (object o, string s) => { };
        public DBContext Context { get { return context; } }

        //Размер добавленной пачки строк, при котором происходит оповещение
        private int cycleSize = 30000;

        public Repository(string dbName) : base(dbName)
        {
        }

        public void DBFill(IEnumerable<SQLiteParameter[]> input)
        {
            DBFillMain(input, cycleSize, null);
            CreateIndexes();
        }

        public void DBFill(ConcurrentQueue<SQLiteParameter[]> input, CancellationTokenSource cts)
        {
            DBFillMain(input, cycleSize, cts);
            CreateIndexes();
        }

        public void DBFill(IParser parser)
        {
            DateTime startOfProcess = DateTime.Now;
            
            Task t1 = parser.ParseAsync();
            Task t2 = Task.Run(() => DBFill(parser.ParametersQueue, parser.Cts));
            Task.WaitAll(t1, t2);
            
            //parser.Parse();
            //DBFill(parser.ParametersQueue, parser.Cts);

            OnEvent(this, $"Операция заполнения базы данных завершена успешно.{Environment.NewLine}" +
                $"Время заполнения базы (мин:сек.сот): {(DateTime.Now - startOfProcess).ToString(@"mm\:ss\.ff")}{Environment.NewLine}");
        }

        private void DBFillMain(IEnumerable<SQLiteParameter[]> input, int cycleSize, CancellationTokenSource cts)
        {
            context.DBConnection.Open();
            //Счетчик циклов, сколько пачек строк уже добавили
            int numberOfCycles = 0;
            //Количество валидных строк в текущем цикле
            int stepOfCurrentCycle = 0;

            ExecuteInnerQuery("PRAGMA synchronous = OFF;PRAGMA journal_mode = OFF; ");

            Func<bool> Condition;
            Func<SQLiteParameter[]> GetData;

            if (input is ConcurrentQueue<SQLiteParameter[]>)
            {
                var queue = (ConcurrentQueue<SQLiteParameter[]>)input;
                Condition = () => !(cts.IsCancellationRequested && queue.Count == 0);
                GetData = () => {
                                    SQLiteParameter[] currentArr;
                                    while (!queue.TryDequeue(out currentArr)) { if (!Condition()) break; }
                                    return currentArr;
                                };
            }
            else
            {
                Condition = () => input.GetEnumerator().MoveNext();
                GetData = () =>input.GetEnumerator().Current;
            }

            using (SQLiteTransaction transaction = context.DBConnection.BeginTransaction())
            {
                using (SQLiteCommand command = new SQLiteCommand(context.DBConnection))
                {
                    command.CommandText = context.InsertionString;
                    while (Condition())
                    {
                        SQLiteParameter[] currentArr = GetData();
                        if (currentArr == null)
                            break;
                        command.Parameters.AddRange(currentArr);
                        command.ExecuteNonQuery();

                        stepOfCurrentCycle++;
                        //Оповещаем о добавлении пачки строк
                        if (stepOfCurrentCycle == cycleSize)
                        {
                            string message = $"\rДобавлено строк: {numberOfCycles * cycleSize}";
                            OnEvent(this, message);
                            numberOfCycles++;
                            stepOfCurrentCycle = 0;
                        }
                    }
                    //Оповещаем о добавлении всех строк
                    if (stepOfCurrentCycle != 0)
                    {
                        string message = $"\rДобавлено строк: {numberOfCycles * cycleSize + stepOfCurrentCycle}{Environment.NewLine}";
                        OnEvent(this, message);
                    }
                }
                transaction.Commit();

            }
            ExecuteInnerQuery("PRAGMA synchronous = NORMAL; PRAGMA journal_mode = DELETE; ");
            
            context.DBConnection.Close();
        }

        public void ExecuteQuery(string query)
        {
            DateTime startOfProcess = DateTime.Now;

            context.DBConnection.Open();
            ExecuteInnerQuery(query);
            context.DBConnection.Close();

            OnEvent(this, $"Время выполнения запроса (мин:сек.сот): {(DateTime.Now - startOfProcess).ToString(@"mm\:ss\.ff")}{Environment.NewLine}");
        }

        private void CreateIndexes()
        {
            DateTime startOfProcess = DateTime.Now;

            OnEvent(this, "Индексируем базу...");
            context.CreateIndexes();

            OnEvent(this, $"Готово.{Environment.NewLine}Время индексирования (мин:сек.сот): {(DateTime.Now - startOfProcess).ToString(@"mm\:ss\.ff")}{Environment.NewLine}");
        }

        private void ExecuteInnerQuery(string query)
        {
            using (SQLiteCommand command = new SQLiteCommand(query, context.DBConnection))
            {
                command.ExecuteNonQuery();
            }
        }

        public DataTable ExecuteQueryResult(string query)
        {
            DateTime startOfProcess = DateTime.Now;

            context.DBConnection.Open();
            
            DataTable table = new DataTable();
            using (SQLiteCommand command = new SQLiteCommand(query, context.DBConnection))
            {
                table.Load(command.ExecuteReader());
            }
            context.DBConnection.Close();

            OnEvent(this, $"Время выполнения запроса (мин:сек.сот): {(DateTime.Now - startOfProcess).ToString(@"mm\:ss\.ff")}{Environment.NewLine}");

            return table;
        }

        public DataTable ExecuteQueryResult(StandardQueries query)
        {
            string queryResult;
            string queryCondition;

            string queryCondition1 = string.Format("{0}1 Вывести количество и сумму заказов по каждому продукту за {0}текущий месяц{0}", Environment.NewLine);
            string queryCondition2a = string.Format("{0}2a Вывести все продукты, которые были заказаны в текущем {0}месяце, но которых не было в прошлом.{0}", Environment.NewLine);
            string queryCondition2b = string.Format("{0}2b Вывести все продукты, которые были только в прошлом месяце,{0}но не в текущем, и которые были в текущем месяце, но не в прошлом.{0}", Environment.NewLine);
            string queryCondition3 = string.Format("{0}3 Помесячно вывести продукт, по которому была максимальная сумма{0}заказов за этот период, сумму по этому продукту и его долю от{0}общего объема за этот период.{0}", Environment.NewLine);

            string query1 = "select product.name as `Продукт`, count(*) as `Кол-во заказов`, sum(`order`.amount) " +
                             "as `Сумма заказов`  from 'order' join product on `product`.id = `order`.product_id " +
                             "where dt_month = strftime('%Y-%m', 'now') group by product.name";

            string tempTable = "select distinct `product`.id as id1, product.name as name1 from 'order' "+
                                "join product on `order`.product_id = `product`.id " +
                                "where dt_month = strftime('%Y-%m', 'now'{0}) ";

            string subQuery = "with lastMonth as (" + string.Format(tempTable, ", '-1 month'") +
                                "), thisMonth as (" + string.Format(tempTable, "") + ") ";

            string query2a = subQuery + "select name1 as `Продукт` from (select * from thisMonth except select * from lastMonth)";

            string query2b = subQuery + "select name1 as `Прошлый месяц`, name2 as `Текущий месяц` from product " +
                            "left outer join (select * from lastMonth except select * from thisMonth) " +
                            "on product.id = id1 left outer join " +
                            "(select id1 as id2, name1 as name2 from thisMonth except select * from lastMonth) " +
                            "on product.id = id2 where name1 not null or name2 not null ";

            string query3 = "select period as `Период`, product_name as `Продукт`, round(max(total_amount),4) as `Сумма`, round(max(total_amount)*100/sum(total_amount),2) as `Доля,%` " +
                            "from(select strftime('%Y-%m', dt) as period, sum(amount) as total_amount, product.name as product_name from `order` " +
                            "join product on `product`.id = `order`.product_id group by product_id, dt_month order by dt_month asc) group by period; ";

            switch (query)
            {
                case StandardQueries.First:
                    queryResult = query1;
                    queryCondition = queryCondition1;
                    break;
                case StandardQueries.SecondA:
                    queryResult = query2a;
                    queryCondition = queryCondition2a;
                    break;
                case StandardQueries.SecondB:
                    queryResult = query2b;
                    queryCondition = queryCondition2b;
                    break;
                case StandardQueries.Third:
                    queryResult = query3;
                    queryCondition = queryCondition3;
                    break;
                default: queryResult = query1;
                    queryCondition = queryCondition1;
                    break;
            }

            OnEvent(this, $"{queryCondition}Выполняется запрос...{Environment.NewLine}");
            return ExecuteQueryResult(queryResult);
        }
    }
}
