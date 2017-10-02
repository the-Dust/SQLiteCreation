using SQLiteCreation.Parsers.Base;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SQLiteCreation.Parsers
{
    class DataVerificationStrategy : IDataVerificationStrategy
    {
        //Хэшсет служит для того, чтобы проверять на уникальность считанные данные столбца "id"
        private HashSet<int> idSet = new HashSet<int>();
        //Нижний предел даты в таблице. С ним будут сравниваться на валидность считанные данные столбца "dt"
        private DateTime startDate = new DateTime(1970, 1, 1);

        public bool Verify(Dictionary<string, int> columnPosition, string[] inputData, int counter, ref string message)
        {
            StringBuilder errorString = new StringBuilder();

            if (inputData.Length >= columnPosition.Count)
            {
                Rearrange(columnPosition, inputData);

                IdVerification(inputData, errorString);
                DTVerification(inputData, errorString);
                ProductIdVerification(inputData, errorString);
                AmountVerification(inputData, errorString);
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

        private void IdVerification(string[] inputData, StringBuilder errorMessage)
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
        }

        private void DTVerification(string[] inputData, StringBuilder errorMessage)
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

            //Пишем дату в буфер в том формате, в котором ее понимает SQLite
            inputData[position] = dt.ToString("s"); //увеличивает время обработки ~на 5,5сек
        }

        private void ProductIdVerification(string[] inputData, StringBuilder errorMessage)
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
        }

        private void AmountVerification(string[] inputData, StringBuilder errorMessage)
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
