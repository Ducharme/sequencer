
using CommonTypes;

namespace DatabaseAccessLayer
{
    public class DatabaseDummyAdmin : IDatabaseAdmin
    {
        public int CreateTable()
        {
            return 1;
        }

        public int DropTable()
        {
            return 1;
        }

        public List<MyMessage> GetAllMessages(string name)
        {
            return new List<MyMessage>();
        }

        public int PurgeTable()
        {
            return 1;
        }
    }
}

