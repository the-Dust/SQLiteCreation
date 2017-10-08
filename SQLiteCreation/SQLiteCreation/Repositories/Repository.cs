using SQLiteCreation.Context.Base;
using SQLiteCreation.Events;
using SQLiteCreation.Parsers.Base;
using SQLiteCreation.Repositories.Base;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Threading;
using System.Threading.Tasks;

namespace SQLiteCreation.Repositories
{
    class Repository : AbstractRepository, IRepository
    {
        public event EventHandler<SQLiteCreationEventArgs> OnEvent = (object sender, SQLiteCreationEventArgs e) => { };
        public event EventHandler<SQLiteCreationEventArgs> OnError = (object sender, SQLiteCreationEventArgs e) => { };

        public IDBContext Context { get { return context; } }

        //Размер добавленной пачки строк, при котором происходит оповещение
        private int cycleSize;
        private string messageOnError = $"При обращении к базе возникла ошибка.{Environment.NewLine}Подробности:{Environment.NewLine}";

        public Repository(IDBContext context, int cycleSize) : base(context)
        {
            this.cycleSize = cycleSize;
        }

        public void DBFill(IEnumerable<SQLiteParameter[]> input)
        {
            try
            {
                DBFillMain(input, cycleSize, null);
            }
            catch (Exception ex)
            {
                CatchingDBException(ex);
            }
            CreateIndexes();
        }

        public void DBFill(ConcurrentQueue<SQLiteParameter[]> input, CancellationTokenSource cts)
        {
            try
            {
                DBFillMain(input, cycleSize, cts);
            }
            catch (Exception ex)
            {
                CatchingDBException(ex);
            }
            CreateIndexes();
        }

        public void DBFill(IParser parser)
        {
            DateTime startOfProcess = DateTime.Now;
            
            Task t1 = parser.ParseAsync();
            Task t2 = Task.Run(() => DBFill(parser.ParametersQueue, parser.Cts));
            Task.WaitAll(t1, t2);

            string message = $"Операция заполнения базы данных завершена успешно.{Environment.NewLine}" +
                $"Время заполнения базы (мин:сек.сот): {(DateTime.Now - startOfProcess).ToString(@"mm\:ss\.ff")}{Environment.NewLine}";
            OnEvent(this, new SQLiteCreationEventArgs(message));
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
            IEnumerator<SQLiteParameter[]> enumerator = input.GetEnumerator();

            if (input is ConcurrentQueue<SQLiteParameter[]>)
            {
                var queue = (ConcurrentQueue<SQLiteParameter[]>)input;
                Condition = () => !(cts.IsCancellationRequested && queue.IsEmpty);
                GetData = () => {
                                    SQLiteParameter[] currentArr;
                                    while (!queue.TryDequeue(out currentArr)) { if (!Condition()) break; }
                                    return currentArr;
                                };
            }
            else
            {
                Condition = () => enumerator.MoveNext();
                GetData = () => enumerator.Current;
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
                            OnEvent(this, new SQLiteCreationEventArgs(message));
                            numberOfCycles++;
                            stepOfCurrentCycle = 0;
                        }
                    }
                    //Оповещаем о добавлении всех строк
                    if (stepOfCurrentCycle != 0)
                    {
                        string message = $"\rДобавлено строк: {numberOfCycles * cycleSize + stepOfCurrentCycle}{Environment.NewLine}";
                        OnEvent(this, new SQLiteCreationEventArgs(message));
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
            try
            {
                ExecuteInnerQuery(query);
            }
            catch (Exception ex)
            {
                OnError(this, new SQLiteCreationEventArgs(messageOnError + ex.Message + $"{Environment.NewLine}Действие не выполнено."));
            }
            context.DBConnection.Close();

            OnEvent(this, new SQLiteCreationEventArgs($"Время выполнения запроса (мин:сек.сот): {(DateTime.Now - startOfProcess).ToString(@"mm\:ss\.ff")}{Environment.NewLine}"));
        }

        private void CreateIndexes()
        {
            DateTime startOfProcess = DateTime.Now;

            OnEvent(this, new SQLiteCreationEventArgs("Индексируем базу..."));
            context.CreateIndexes();

            OnEvent(this, new SQLiteCreationEventArgs($"Готово.{Environment.NewLine}Время индексирования (мин:сек.сот): {(DateTime.Now - startOfProcess).ToString(@"mm\:ss\.ff")}{Environment.NewLine}"));
        }

        private void ExecuteInnerQuery(string query)
        {
            using (SQLiteCommand command = new SQLiteCommand(query, context.DBConnection))
            {
                command.ExecuteNonQuery();
            }
        }

        private void CatchingDBException(Exception ex)
        {
            string message = $"При заполнении базы возникла ошибка.{Environment.NewLine}Подробности:{Environment.NewLine}";
            OnError(this, new SQLiteCreationEventArgs(messageOnError + ex.Message + $"{Environment.NewLine}Действие не выполнено."));
            context.DBConnection.Close();
        }

        public DataTable ExecuteQueryResult(string query)
        {
            DateTime startOfProcess = DateTime.Now;

            context.DBConnection.Open();
            
            DataTable table = new DataTable();
            using (SQLiteCommand command = new SQLiteCommand(query, context.DBConnection))
            {
                try
                {
                    table.Load(command.ExecuteReader());
                }
                catch (Exception ex)
                {
                    OnError(this, new SQLiteCreationEventArgs(messageOnError + ex.Message + $"{Environment.NewLine}Действие не выполнено."));
                }
            }
            context.DBConnection.Close();

            OnEvent(this, new SQLiteCreationEventArgs($"Время выполнения запроса (мин:сек.сот): {(DateTime.Now - startOfProcess).ToString(@"mm\:ss\.ff")}{Environment.NewLine}"));

            return table;
        }

        public DataTable ExecuteQueryResult(StandardQueries query)
        {
            var queryResult = context.StandardDBQuery.GetQuery(query);

            OnEvent(this, new SQLiteCreationEventArgs($"{queryResult.Key}Выполняется запрос...{Environment.NewLine}"));
            return ExecuteQueryResult(queryResult.Value);
        }
    }
}
