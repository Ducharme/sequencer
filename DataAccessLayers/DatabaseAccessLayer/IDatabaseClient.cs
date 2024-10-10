using CommonTypes;

namespace DatabaseAccessLayer
{
    public interface IDatabaseClient
    {
        bool CanMessageBeProcessed(string name, long sequence);
        DateTime InsertMessages<M>(List<M> mms) where M : MyMessage;
    }
}


