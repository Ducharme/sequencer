using CommonTypes;

namespace DatabaseAccessLayer
{
    public class DatabaseDummyClient : IDatabaseClient
    {
        public bool CanMessageBeProcessed(string name, long sequence)
        {
            return true;
        }

        public DateTime InsertMessages(List<MyMessage> mms)
        {
            var now = DateTime.Now;
            var utcOffset = TimeZoneInfo.Local.GetUtcOffset(now);
            var savedAt = now.Subtract(utcOffset);
            return savedAt;
        }
    }
}


