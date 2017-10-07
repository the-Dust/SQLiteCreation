using SQLiteCreation.Controllers.Base;
using System;
using System.Configuration;
using System.IO;

namespace SQLiteCreation
{
    class Program
    {
        static void Main()
        {
            string controllerType = ConfigurationManager.AppSettings.Get("ControllerType");
            string outputDBFileName = ConfigurationManager.AppSettings.Get("OutputDBFileName");

            string tSVFileName = ConfigurationManager.AppSettings.Get("TSVFileName");
            if (!File.Exists(tSVFileName))
            {
                Console.WriteLine("Вы указали неверный путь к файлу \".tsv\"");
                QuitOnWrongData(1);
            }

            int cycleSizeToDisplay;
            if (!int.TryParse(ConfigurationManager.AppSettings.Get("CycleSizeToDisplay"), out cycleSizeToDisplay) || cycleSizeToDisplay<=0)
            {
                Console.WriteLine("Параметр \"CycleSizeToDisplay\" введен неверно");
                Console.WriteLine("Введите целое число больше нуля");
                QuitOnWrongData(2);
            }

            Type tControllerType = null;
            try
            {
                tControllerType = Type.GetType(controllerType);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Произошла ошибка при создании ядра программы.");
                Console.WriteLine("Подробности:");
                Console.WriteLine(ex.Message);
                QuitOnWrongData(3);
            }

            if (tControllerType == null)
            {
                Console.WriteLine("Указано неверное имя контроллера.");
                QuitOnWrongData(3);
            }

            IController controller = (IController)Activator.CreateInstance(tControllerType, tSVFileName, cycleSizeToDisplay, outputDBFileName);

            string message = $"------------------------------------------------------------------{Environment.NewLine}"
                            + $"Приложение запущено{Environment.NewLine}" +
                            $"------------------------------------------------------------------{Environment.NewLine}";

            controller.SendData(message);
            controller.SendData($"Выполняется заполнение базы данных{Environment.NewLine}");
            controller.FillDataBase();

            
            string userAction = "";
            while (true)
            {
                controller.SendData($"{Environment.NewLine}Выполнить запрос к базе? ([любая клавиша/N] + Enter){Environment.NewLine}");
                userAction = controller.GetData();
                if (userAction == "N")
                    break;
                controller.SendData($"Введите номер запроса (1/2a/2b/3){Environment.NewLine}");
                controller.GetQuery(controller.GetData());
            }

            controller.SendData("Работа приложения завершена");
            Console.ReadLine();
            
        }

        static void QuitOnWrongData(int index)
        {
            Console.WriteLine("Проверьте правильность ввода в файле App.config");
            Console.WriteLine("Нажмите любую клавишу для закрытия приложения");
            Console.ReadKey();
            Environment.Exit(index);
        }
    }
}
