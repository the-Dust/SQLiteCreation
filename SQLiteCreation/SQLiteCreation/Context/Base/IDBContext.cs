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
        event Action<object, string> OnFatalError;

        SQLiteConnection DBConnection { get; }
        string[] Headers { get; }
        string InsertionString { get; }
        IDBQuery StandardDBQuery { get; }

        void CreateIndexes();
    }
}
