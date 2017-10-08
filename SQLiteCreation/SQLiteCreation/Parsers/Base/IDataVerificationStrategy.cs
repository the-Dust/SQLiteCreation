using System.Collections.Generic;
using System.Data.SQLite;

namespace SQLiteCreation.Parsers.Base
{
    interface IDataVerificationStrategy
    {
        int ParametersCount { get; }

        bool Verify(Dictionary<string, int> columnPosition, string[] inputData, SQLiteParameter[] parameters, int counter, ref string message);
    }
}
