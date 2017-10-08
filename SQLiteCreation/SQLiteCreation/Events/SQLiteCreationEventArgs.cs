using System;

namespace SQLiteCreation.Events
{
    class SQLiteCreationEventArgs : EventArgs
    {
        public string Message { get; }

        public SQLiteCreationEventArgs(string message)
        {
            Message = message;
        }
    }
}
