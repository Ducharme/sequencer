using CommonTypes;

namespace DatabaseAccessLayer
{
    public interface IDatabaseAdmin
    {
        List<MyMessage> GetAllMessages(string name);
        int CreateTable();
        int PurgeTable();
        int DropTable();
    }
}


