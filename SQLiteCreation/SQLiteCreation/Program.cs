using SQLiteCreation.Controllers;
using SQLiteCreation.Controllers.Base;
using System;

namespace SQLiteCreation
{
    class Program
    {
        static void Main()
        {
            string name = "WriteLines.tsv";

            string message = $"------------------------------------------------------------------{Environment.NewLine}"
                            + $"Приложение запущено{Environment.NewLine}" +
                            $"------------------------------------------------------------------{Environment.NewLine}";

            IController controller = new Controller(name, 30000);
            controller.SendData(message);
            controller.SendData($"Выполняется заполнение базы данных{Environment.NewLine}");
            controller.FillDataBase();

            
            string userAction = "";
            while (true)
            {
                controller.SendData($"{Environment.NewLine}Выполнить запрос к базе? (любая клавиша/N){Environment.NewLine}");
                userAction = controller.GetData();
                if (userAction == "N")
                    break;
                controller.SendData($"Введите номер запроса (1/2a/2b/3){Environment.NewLine}");
                controller.GetQuery(controller.GetData());
            }

            controller.SendData("Работа приложения завершена");
            Console.ReadLine();
            
        }
    }
}
