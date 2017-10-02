using SQLiteCreation.DataWiewers.Base;
using System;
using System.Data;
using System.Text;

namespace SQLiteCreation.DataWiewers
{
    class DataViewer : IDataViewer
    {
        private Action<string> dataPrinter;
        private Func<string> dataReceiver;

        public DataViewer(Action<string> dataPrinter, Func<string> dataReceiver)
        {
            this.dataPrinter = dataPrinter;
            this.dataReceiver = dataReceiver;
        }

        public void ViewData(DataTable table)
        {
            StringBuilder sb = new StringBuilder();

            if (table.Rows.Count < 1)
                sb.Append($"{Environment.NewLine}Таблица не содержит строк{Environment.NewLine}");
            else
            {
                foreach (DataColumn column in table.Columns)
                    sb.Append(string.Format(" {0, -15}|", column.ColumnName));
                sb.Append($"\t{Environment.NewLine}");

                //Начертим разделитель между строкой заголовка и данными
                int length = table.Columns.Count;
                string limiter = new string('-', length * 17);
                sb.Append($"{limiter}{Environment.NewLine}");

                foreach (DataRow row in table.Rows)
                {
                    foreach (var item in row.ItemArray)
                        sb.Append(string.Format(" {0, -15}|", item));
                    sb.Append($"\t{Environment.NewLine}");
                }
            }

            ViewData(sb.ToString());
        }

        public void ViewData(string data)
        {
            dataPrinter(data);
        }

        public string ReceiveData()
        {
            return dataReceiver();
        }
    }
}
