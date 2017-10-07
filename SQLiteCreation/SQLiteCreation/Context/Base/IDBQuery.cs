using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLiteCreation.Context.Base
{
    interface IDBQuery
    {
        KeyValuePair<string, string> GetQuery(StandardQueries query);
    }

    enum StandardQueries
    {
        First,
        SecondA,
        SecondB,
        Third
    }
}
