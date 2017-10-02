using System.Data;

namespace SQLiteCreation.DataWiewers.Base
{
    interface IDataViewer
    {
        void ViewData(DataTable table);
        void ViewData(string data);
        string ReceiveData();
    }
}
