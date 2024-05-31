using DatabaseAL;
using Npgsql;

namespace DatabaseAccessLayer
{
    public class DatabaseConnection : IDatabaseConnection
    {
        public NpgsqlConnection Connection { get; }

        public DatabaseConnection(string connectionString)
        {
            Connection = new NpgsqlConnection(connectionString);
        }

        public IDatabaseCommand CreateCommand(string sql)
        {
            return new DatabaseCommand(sql, this);
        }

        public void Open()
        {
            Connection.Open();
        }

        public void Close()
        {
            Connection.Close();
        }

        public void Dispose()
        {
            Connection.Dispose();
        }
    }
}