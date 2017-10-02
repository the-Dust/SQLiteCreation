using SQLiteCreation.Context;

namespace SQLiteCreation.Repositories.Base
{
    abstract class AbstractRepository
    {
        protected DBContext context;

        protected AbstractRepository(string dbName)
        {
            context = new DBContext(dbName);
        }

        protected AbstractRepository(DBContext context)
        {
            this.context = context;
        }
    }
}
