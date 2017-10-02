using System;
using System.Data.SQLite;

namespace SQLiteCreation.Context
{
    class DBContext
    {
        public event Action<object, string> OnFatalError = (object o, string s) => { };
        public SQLiteConnection DBConnection { get; private set; }
        public string[] Headers { get; } = { "id", "dt", "product_id", "amount" };
        public string InsertionString { get; } = $"insert into 'order' (id, dt, product_id, amount) values ";
        public string InsertionTemplate { get; } = "({0},'{1}',{2},{3}),";

        public DBContext(string dbName = "MyDatabase.sqlite")
        {
            try
            {
                DBSetup(dbName);
            }
            catch (Exception ex)
            {
                string message = $"В процессе работы с базой данных возникла ошибка.{Environment.NewLine}Подробности:"
                                    + ex.Message + $"{Environment.NewLine}База данных создана неполностью.";
                OnFatalError(this, message);
            }
        }

        private void DBSetup(string dbName)
        {
            SQLiteConnection.CreateFile(dbName);
            //Создаем подключение и подключаемся к базе
            DBConnection = new SQLiteConnection($"Data Source={dbName};Version=3;");

            DBConnection.Open();

            //Создаем и заполняем таблицу product по условию задачи
            string sql = "CREATE TABLE product (id int primary key not null, name text) without rowid";
            SQLiteCommand command = new SQLiteCommand(sql, DBConnection);
            command.ExecuteNonQuery();

            sql = "insert into product values (1, 'A'), (2, 'B'), (3, 'C'), (4, 'D'), (5, 'E'), (6, 'F'), (7, 'G');";
            command = new SQLiteCommand(sql, DBConnection);
            command.ExecuteNonQuery();

            //Создаем и заполняем таблицу order
            sql = $"CREATE TABLE 'order' ({Headers[0]} int primary key not null, {Headers[1]} datetime not null, {Headers[2]} int not null, {Headers[3]} real not null, foreign key(product_id) references product(id))";
            command = new SQLiteCommand(sql, DBConnection);
            command.ExecuteNonQuery();
            /*
            //Индексируем столбцы таблицы order
            sql = $"CREATE INDEX index1 ON 'order' ({Headers[2]});";
            command = new SQLiteCommand(sql, DBConnection);
            command.ExecuteNonQuery();
            */
            DBConnection.Close();
        }
    }
}
