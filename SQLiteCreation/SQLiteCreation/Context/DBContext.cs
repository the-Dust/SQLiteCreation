using SQLiteCreation.Context.Base;
using SQLiteCreation.Events;
using System;
using System.Data.SQLite;

namespace SQLiteCreation.Context
{
    class DBContext : IDBContext
    {
        public event EventHandler<SQLiteCreationEventArgs> OnFatalError = (object sender, SQLiteCreationEventArgs e) => { };
        public event EventHandler<SQLiteCreationEventArgs> OnError = (object sender, SQLiteCreationEventArgs e) => { };
        public SQLiteConnection DBConnection { get; private set; }
        public string[] Headers { get; } = { "id", "dt", "product_id", "amount" };
        public string InsertionString { get; } = "insert into 'order' (id, dt, product_id, amount, dt_month) values (?1, ?2, ?3, ?4, strftime('%Y-%m', ?2))";
        public IDBQuery StandardDBQuery { get; }

        public DBContext(IDBQuery standardDBQuery, string dbName = "MyDatabase.sqlite")
        {
            StandardDBQuery = standardDBQuery;
            try
            {
                DBSetup(dbName);
            }
            catch (Exception ex)
            {
                string message = $"В процессе работы с базой данных возникла ошибка.{Environment.NewLine}Подробности:{Environment.NewLine}"
                                    + ex.Message + $"{Environment.NewLine}База данных создана неполностью.";
                OnFatalError(this, new SQLiteCreationEventArgs(message));
            }
        }

        public void CreateIndexes()
        {
            DBConnection.Open();
            using (SQLiteCommand command = new SQLiteCommand(DBConnection))
            {
                command.CommandText = $"CREATE INDEX dt_index ON 'order' ({Headers[1]}_month, product_id);";
                try
                {
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    string message = $"При индексировании базы возникла ошибка.{Environment.NewLine}Подробности:{Environment.NewLine}"
                                    + ex.Message + $"{Environment.NewLine}Индексы не созданы.";
                    OnError(this, new SQLiteCreationEventArgs(message));
                }
                
            }
            DBConnection.Close();
        }

        private void DBSetup(string dbName)
        {
            SQLiteConnection.CreateFile(dbName);
            //Создаем подключение и подключаемся к базе
            DBConnection = new SQLiteConnection($"Data Source={dbName};Version=3;");

            DBConnection.Open();
            using (SQLiteCommand command = new SQLiteCommand(DBConnection))
            { 
                //Создаем и заполняем таблицу product по условию задачи
                command.CommandText = "CREATE TABLE product (id int primary key not null, name text) without rowid";
                command.ExecuteNonQuery();
            
                command.CommandText = "insert into product values (1, 'A'), (2, 'B'), (3, 'C'), (4, 'D'), (5, 'E'), (6, 'F'), (7, 'G');";
                command.ExecuteNonQuery();

                //Создаем и заполняем таблицу order
                command.CommandText = $"CREATE TABLE 'order' ({Headers[0]} int primary key not null, {Headers[1]} datetime not null, {Headers[2]} int not null, {Headers[3]} real not null, {Headers[1]}_month datetime, foreign key(product_id) references product(id))";
                command.ExecuteNonQuery();
            }
            DBConnection.Close();
        }
    }
}
