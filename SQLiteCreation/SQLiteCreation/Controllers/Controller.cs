using SQLiteCreation.Controllers.Base;
using SQLiteCreation.DataWiewers;
using SQLiteCreation.DataWiewers.Base;
using SQLiteCreation.Parsers;
using SQLiteCreation.Parsers.Base;
using SQLiteCreation.Repositories;
using SQLiteCreation.Repositories.Base;
using System;
using System.Data;
using System.Threading;

namespace SQLiteCreation.Controllers
{
    class Controller : IController
    {
        private IRepository repository;
        private IParser parser;
        private IDataViewer viewer;
        private int cycleSize;

        public Controller(string pathToFile, int cycleSize = 20000, string dbFilename = "MyDatabase.sqlite")
        {
            repository = new Repository(dbFilename);
            parser = new Parser(pathToFile, new string[] { "\t" }, repository.Context.Headers, new DataVerificationStrategy());
            viewer = new DataViewer(Console.Write, Console.ReadLine);
            this.cycleSize = cycleSize;
            repository.OnEvent += EventHandling;
            repository.OnError += ErrorHandling;
            repository.Context.OnFatalError += FatalErrorHandling;
            //parser.OnError+= ErrorHandling;
            parser.OnFatalError += FatalErrorHandling;
        }

        public void FillDataBase()
        {
            repository.DBFill(parser, cycleSize);
        }

        public void ErrorHandling(object sender, string message)
        {
            viewer.ViewData(message);
        }

        public void EventHandling(object sender, string message)
        {
            viewer.ViewData(message);
        }

        public void FatalErrorHandling(object sender, string message)
        {
            viewer.ViewData(message);
            Thread.Sleep(3000);
            Environment.Exit(0);
        }

        public void GetQuery(string query)
        {
            DataTable table;

            switch (query)
            {
                case "1": table = repository.ExecuteQueryResult(StandardQueries.First); break;
                case "2a": table = repository.ExecuteQueryResult(StandardQueries.SecondA); break;
                case "2b": table = repository.ExecuteQueryResult(StandardQueries.SecondB); break;
                case "3": table = repository.ExecuteQueryResult(StandardQueries.Third); break;
                default: table = repository.ExecuteQueryResult(StandardQueries.First); break;
            }

            viewer.ViewData(table);
        }

        public void SendData(string message)
        {
            viewer.ViewData(message);
        }

        public string GetData()
        {
            return viewer.ReceiveData();
        }
    }
}
