using SQLiteCreation.Context;
using SQLiteCreation.Parsers.Base;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Threading;

namespace SQLiteCreation.Repositories.Base
{
    interface IRepository
    {
        void DBFill(IEnumerable<string[]> array, int cycleSize);
        void DBFill(ConcurrentQueue<string[]> queue, int cycleSize, CancellationTokenSource cts);
        void DBFill(IParser parser, int cycleSize);
        void ExecuteQuery(string query);
        DataTable ExecuteQueryResult(string query);
        DataTable ExecuteQueryResult(StandardQueries query);
        event Action<object, string> OnError;
        event Action<object, string> OnEvent;
        DBContext Context { get; }
    }

    enum StandardQueries
    {
        First,
        SecondA,
        SecondB,
        Third
    }
}
