using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SQLiteCreation.Parsers.Base
{
    interface IParser
    {
        ConcurrentQueue<string[]> StringArrayQueue { get; }
        CancellationTokenSource Cts { get; }
        event Action<object, string> OnError;
        event Action<object, string> OnFatalError;

        void Parse();
        Task ParseAsync();
    }
}
