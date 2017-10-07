using SQLiteCreation.Events;
using System;
using System.Collections.Concurrent;
using System.Data.SQLite;
using System.Threading;
using System.Threading.Tasks;

namespace SQLiteCreation.Parsers.Base
{
    interface IParser
    {
        event EventHandler<SQLiteCreationEventArgs> OnError;
        event EventHandler<SQLiteCreationEventArgs> OnFatalError;

        ConcurrentQueue<SQLiteParameter[]> ParametersQueue { get; }
        CancellationTokenSource Cts { get; }

        void Parse();
        Task ParseAsync();
    }
}
