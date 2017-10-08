using SQLiteCreation.Events;

namespace SQLiteCreation.Controllers.Base
{
    interface IController
    {
        void ErrorHandling(object sender, SQLiteCreationEventArgs e);
        void FatalErrorHandling(object sender, SQLiteCreationEventArgs e);
        void EventHandling(object sender, SQLiteCreationEventArgs e);

        void FillDataBase();
        void GetQuery(string query);
        void SendData(string message);
        string GetData();
    }
}
