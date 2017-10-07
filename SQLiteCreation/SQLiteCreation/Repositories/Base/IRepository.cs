using SQLiteCreation.Context;
using SQLiteCreation.Parsers.Base;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Threading;
using SQLiteCreation.Context.Base;

namespace SQLiteCreation.Repositories.Base
{
    interface IRepository
    {
        void DBFill(IEnumerable<SQLiteParameter[]> array);
        void DBFill(ConcurrentQueue<SQLiteParameter[]> queue, CancellationTokenSource cts);
        void DBFill(IParser parser);
        void ExecuteQuery(string query);
        DataTable ExecuteQueryResult(string query);
        DataTable ExecuteQueryResult(StandardQueries query);
        event Action<object, string> OnError;
        event Action<object, string> OnEvent;
        IDBContext Context { get; }
    }
}
