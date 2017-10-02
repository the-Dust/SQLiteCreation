namespace SQLiteCreation.Controllers.Base
{
    interface IController
    {
        void ErrorHandling(object sender, string message);
        void FatalErrorHandling(object sender, string message);
        void EventHandling(object sender, string message);

        void FillDataBase();
        void GetQuery(string query);
        void SendData(string message);
        string GetData();
    }
}
