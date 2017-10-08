using SQLiteCreation.Context;
using SQLiteCreation.Context.Base;
using SQLiteCreation.Controllers.Base;
using SQLiteCreation.DataWiewers;
using SQLiteCreation.DataWiewers.Base;
using SQLiteCreation.Events;
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

        public Controller(string pathToFile, int cycleSize, string dbFilename)
        {
            IDBContext context = new DBContext(new DBQuery(), dbFilename);
            repository = new Repository(context, cycleSize);
            parser = new Parser(pathToFile, new string[] { "\t" }, repository.Context.Headers, new DataVerificationStrategy());
            viewer = new DataViewer(Console.Write, Console.ReadLine);
            this.cycleSize = cycleSize;
            repository.OnEvent += EventHandling;
            repository.OnError += ErrorHandling;
            repository.Context.OnFatalError += FatalErrorHandling;
            repository.Context.OnError += ErrorHandling;
            //parser.OnError+= ErrorHandling;
            parser.OnFatalError += FatalErrorHandling;
        }

        public void FillDataBase()
        {
            repository.DBFill(parser);
        }

        public void ErrorHandling(object sender, SQLiteCreationEventArgs e)
        {
            viewer.ViewData(e.Message);
        }

        public void EventHandling(object sender, SQLiteCreationEventArgs e)
        {
            viewer.ViewData(e.Message);
        }

        public void FatalErrorHandling(object sender, SQLiteCreationEventArgs e)
        {
            viewer.ViewData(e.Message);
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
