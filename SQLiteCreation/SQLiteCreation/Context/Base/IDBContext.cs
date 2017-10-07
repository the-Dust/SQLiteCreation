using SQLiteCreation.Events;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLiteCreation.Context.Base
{
    interface IDBContext
    {
        event EventHandler<SQLiteCreationEventArgs> OnFatalError;
        event EventHandler<SQLiteCreationEventArgs> OnError;

        SQLiteConnection DBConnection { get; }
        string[] Headers { get; }
        string InsertionString { get; }
        IDBQuery StandardDBQuery { get; }

        void CreateIndexes();
    }
}
