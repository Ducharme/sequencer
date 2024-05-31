using DatabaseAL;

namespace DatabaseAccessLayer
{
    public interface IDatabaseConnectionFetcher
    {
        IDatabaseConnection GetNewConnection();
    }
}