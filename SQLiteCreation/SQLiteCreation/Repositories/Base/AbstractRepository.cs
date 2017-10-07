using SQLiteCreation.Context;
using SQLiteCreation.Context.Base;

namespace SQLiteCreation.Repositories.Base
{
    abstract class AbstractRepository
    {
        protected IDBContext context;

        protected AbstractRepository(IDBQuery query, string dbName)
        {
            context = new DBContext(query, dbName);
        }

        protected AbstractRepository(IDBContext context)
        {
            this.context = context;
        }
    }
}
