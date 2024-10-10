using CommonTypes;

namespace DatabaseAccessLayer
{
    public class DatabaseDummyClient : IDatabaseClient
    {
        public bool CanMessageBeProcessed(string name, long sequence)
        {
            return true;
        }

        public DateTime InsertMessages<M>(List<M> mms) where M : MyMessage
        {
            var now = DateTime.Now;
            var utcOffset = TimeZoneInfo.Local.GetUtcOffset(now);
            var savedAt = now.Subtract(utcOffset);
            return savedAt;
        }
    }
}


