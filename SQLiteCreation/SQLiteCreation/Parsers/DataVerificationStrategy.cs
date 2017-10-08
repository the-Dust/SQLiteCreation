using SQLiteCreation.Parsers.Base;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Globalization;
using System.Text;

namespace SQLiteCreation.Parsers
{
    class DataVerificationStrategy : IDataVerificationStrategy
    {
        public int ParametersCount { get; } = 4;

        //Хэшсет служит для того, чтобы проверять на уникальность считанные данные столбца "id"
        private HashSet<int> idSet = new HashSet<int>();
        //Нижний предел даты в таблице. С ним будут сравниваться на валидность считанные данные столбца "dt"
        private DateTime startDate = new DateTime(1970, 1, 1);

        public bool Verify(Dictionary<string, int> columnPosition, string[] inputData, SQLiteParameter[] parameters, int counter, ref string message)
        {
            StringBuilder errorString = new StringBuilder();

            if (inputData.Length >= columnPosition.Count)
            {
                Rearrange(columnPosition, inputData);

                IdVerification(inputData, errorString, parameters);
                DTVerification(inputData, errorString, parameters);
                ProductIdVerification(inputData, errorString, parameters);
                AmountVerification(inputData, errorString, parameters);
            }
            else
                errorString.Append("- отсутствуют некоторые столбцы с данными" + Environment.NewLine);

            if (errorString.Length > 0)
            {
                message = string.Format("*********{0}В строке {1} обнаружены следующие ошибки:{0}{2}Данная строка будет проигнорирована{0}", Environment.NewLine, counter, errorString);
                return false;
            }
            else return true;
        }

        private void IdVerification(string[] inputData, StringBuilder errorMessage, SQLiteParameter[] parameters)
        {
            int id = 0;
            int position = 0; //id

            if (!int.TryParse(inputData[position], out id))
            {
                if (string.IsNullOrWhiteSpace(inputData[position]))
                    errorMessage.Append("- Значение id не указано" + Environment.NewLine);
                else
                    errorMessage.Append("- id не является числом Int32" + Environment.NewLine);
            }
            else if (id < 0)
            {
                errorMessage.Append("- id имеет отрицательное значение" + Environment.NewLine);
            }
            else if (!idSet.Add(id))
            {
                errorMessage.Append("- id имеет неуникальное значение" + Environment.NewLine);
            }
            else
                parameters[0] = new SQLiteParameter("1", DbType.Int32) { Value=id};
        }

        private void DTVerification(string[] inputData, StringBuilder errorMessage, SQLiteParameter[] parameters)
        {
            DateTime dt;
            int position = 1; //dt

            if (!DateTime.TryParse(inputData[position], out dt))
            {
                if (string.IsNullOrWhiteSpace(inputData[position]))
                    errorMessage.Append("- Значение dt не указано" + Environment.NewLine);
                else
                    errorMessage.Append("- dt имеет неверный формат даты" + Environment.NewLine);
            }
            else if (dt < startDate)
            {
                errorMessage.Append("- Указана дата ранее 1970-01-01 (по условиям задачи)" + Environment.NewLine);
            }
            else if (dt > DateTime.Now)
            {
                errorMessage.Append("- Указанная дата еще не наступила" + Environment.NewLine);
            }
            else
            {
                parameters[1] = new SQLiteParameter("2", DbType.DateTime) { Value = dt };
            }
        }

        private void ProductIdVerification(string[] inputData, StringBuilder errorMessage, SQLiteParameter[] parameters)
        {
            int productId = 0;
            int position = 2; //product_id

            if (!int.TryParse(inputData[position], out productId))
            {
                if (string.IsNullOrWhiteSpace(inputData[position]))
                    errorMessage.Append("- Значение productId не указано" + Environment.NewLine);
                else
                    errorMessage.Append("- product_id не является числом Int32" + Environment.NewLine);
            }
            else if (productId < 1 || productId > 7)
            {
                errorMessage.Append("- product_id не является значением id из таблицы \"product\"" + Environment.NewLine);
            }
            else
                parameters[2] = new SQLiteParameter("3", DbType.Int32) { Value = productId };
        }

        private void AmountVerification(string[] inputData, StringBuilder errorMessage, SQLiteParameter[] parameters)
        {
            //Флаг того, что распознать значение amount не удалось
            bool amountFail = false;
            int position = 3; //amount
            float amount = 0;

            try
            {
                amount = Single.Parse(inputData[position], CultureInfo.InvariantCulture);
            }
            catch
            {
                amountFail = true;
                if (string.IsNullOrWhiteSpace(inputData[position]))
                    errorMessage.Append("- Значение amount не указано" + Environment.NewLine);
                else
                    errorMessage.Append("- amount не является числом real" + Environment.NewLine);
            }
            if (!amountFail & amount < 0)
            {
                errorMessage.Append("- amount имеет отрицательное значение" + Environment.NewLine);
            }
            else
                parameters[3] = new SQLiteParameter("4", DbType.Single) { Value = amount };
        }

        private void Rearrange(Dictionary<string, int> columnPosition, string[] inputData)
        {
            string id = inputData[columnPosition["id"]];
            string dt = inputData[columnPosition["dt"]];
            string productId = inputData[columnPosition["product_id"]];
            string amount = inputData[columnPosition["amount"]];

            inputData[0] = id;
            inputData[1] = dt;
            inputData[2] = productId;
            inputData[3] = amount;
        }
    }
}
