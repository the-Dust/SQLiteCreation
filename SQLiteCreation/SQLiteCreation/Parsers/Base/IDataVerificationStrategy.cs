using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLiteCreation.Parsers.Base
{
    interface IDataVerificationStrategy
    {
        bool Verify(Dictionary<string, int> columnPosition, string[] inputData, int counter, ref string message);
    }
}
