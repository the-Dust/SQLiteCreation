using System;
using System.Collections.Concurrent;
using System.Data.SQLite;
using System.Threading;
using System.Threading.Tasks;

namespace SQLiteCreation.Parsers.Base
{
    interface IParser
    {
        ConcurrentQueue<SQLiteParameter[]> ParametersQueue { get; }
        CancellationTokenSource Cts { get; }
        event Action<object, string> OnError;
        event Action<object, string> OnFatalError;

        void Parse();
        Task ParseAsync();
    }
}
