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
        }

        public void DBFill(ConcurrentQueue<SQLiteParameter[]> input, CancellationTokenSource cts)
        {
            DBFillMain(input, cycleSize, cts);
        }

        public void DBFill(IParser parser)
        {
            DateTime startOfProcess = DateTime.Now;
            
            Task t1 = parser.ParseAsync();
            Task t2 = Task.Run(() => DBFill(parser.ParametersQueue, parser.Cts));
            Task.WaitAll(t1, t2);
            
            OnEvent(this, $"Операция заполнения базы данных завершена успешно.{Environment.NewLine}" +
                $"Время выполнения операции (мин:сек.сот): {(DateTime.Now - startOfProcess).ToString(@"mm\:ss\.ff")}{Environment.NewLine}");
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
                                    while (!queue.TryDequeue(out currentArr)) { }
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
                    command.CommandText = "insert into 'order' (id, dt, product_id, amount) values (?1, ?2, ?3, ?4)";

                    while (Condition())
                    {
                        command.Parameters.AddRange(GetData());
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

            string queryCondition1 = string.Format("1 Вывести количество и сумму заказов по каждому продукту за {0}текущий месяц{0}", Environment.NewLine);
            string queryCondition2a = string.Format("2a Вывести все продукты, которые были заказаны в текущем {0}месяце, но которых не было в прошлом.{0}", Environment.NewLine);
            string queryCondition2b = string.Format("2b Вывести все продукты, которые были только в прошлом месяце,{0}но не в текущем, и которые были в текущем месяце, но не в прошлом.{0}", Environment.NewLine);
            string queryCondition3 = string.Format("3 Помесячно вывести продукт, по которому была максимальная сумма{0}заказов за этот период, сумму по этому продукту и его долю от{0}общего объема за этот период.{0}", Environment.NewLine);

            string query1 = "select product.name as `Продукт`, count(*) as `Кол-во заказов`, sum(`order`.amount) " +
                             "as `Сумма заказов`  from 'order' join product on `product`.id = `order`.product_id " +
                             "where strftime('%Y-%m', dt) = strftime('%Y-%m', 'now') group by product.name";

            string query2a = "select distinct product.name as `Продукт` from 'order' " +
                            "join product on `product`.id = `order`.product_id " +
                            "where strftime('%Y-%m', dt) = strftime('%Y-%m', 'now') " +
                            "except " +
                            "select distinct product.name as `Продукт` from 'order' " +
                            "join product on `product`.id = `order`.product_id " +
                            "where strftime('%Y-%m', dt) = strftime('%Y-%m', 'now', '-1 month')";

            string query2b = "select name2 as `Прошлый месяц`, name3 as `Текущий месяц` from product " +
                            "left outer join " +
                            "(select distinct `product`.id as id1, product.name as name2 from 'order' " +
                            "join product on `product`.id = `order`.product_id " +
                            "where strftime('%Y-%m', dt) = strftime('%Y-%m', 'now', '-1 month') " +
                            "except " +
                            "select distinct `product`.id as id1, product.name as name2 from 'order' " +
                            "join product on `product`.id = `order`.product_id " +
                            "where strftime('%Y-%m', dt) = strftime('%Y-%m', 'now')) " +
                            "on id1 = product.id " +
                            "left outer join " +
                            "(select distinct `product`.id as id2, product.name as name3 from 'order' " +
                            "join product on `product`.id = `order`.product_id " +
                            "where strftime('%Y-%m', dt) = strftime('%Y-%m', 'now') " +
                            "except " +
                            "select distinct `product`.id as id2, product.name as name3 from 'order' " +
                            "join product on `product`.id = `order`.product_id " +
                            "where strftime('%Y-%m', dt) = strftime('%Y-%m', 'now', '-1 month')) " +
                            "on id2 = product.id where name2 not null or name3 not null";

            string query3 = "select period as `Период`, product_name as `Продукт`, round(max(total_amount),4) as `Сумма`, round(max(total_amount)*100/sum(total_amount),2) as `Доля,%` " +
                            "from(select strftime('%Y-%m', dt) as period, sum(amount) as total_amount, product.name as product_name from `order` " +
                            "join product on `product`.id = `order`.product_id group by product.name, strftime('%Y-%m', dt) order by strftime('%Y-%m', dt) asc) group by period order by period asc; ";

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

            OnEvent(this, queryCondition);
            return ExecuteQueryResult(queryResult);
        }
    }
}
