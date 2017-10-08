using SQLiteCreation.Context.Base;
using SQLiteCreation.Events;
using SQLiteCreation.Parsers.Base;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Threading;

namespace SQLiteCreation.Repositories.Base
{
    interface IRepository
    {
        event EventHandler<SQLiteCreationEventArgs> OnError;
        event EventHandler<SQLiteCreationEventArgs> OnEvent;

        void DBFill(IEnumerable<SQLiteParameter[]> array);
        void DBFill(ConcurrentQueue<SQLiteParameter[]> queue, CancellationTokenSource cts);
        void DBFill(IParser parser);
        void ExecuteQuery(string query);
        DataTable ExecuteQueryResult(string query);
        DataTable ExecuteQueryResult(StandardQueries query);

        IDBContext Context { get; }
    }
}
