using Npgsql;

namespace DatabaseAL
{
    public interface IDatabaseConnection : IDisposable
    {
        void Open();
        void Close();

        IDatabaseCommand CreateCommand(string sql);

        NpgsqlConnection Connection { get; }
    }
}
