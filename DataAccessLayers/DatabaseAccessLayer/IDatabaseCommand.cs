using Npgsql;
using NpgsqlTypes;

namespace DatabaseAL
{
    public interface IDatabaseCommand
    {
        object? ExecuteScalar();
        int ExecuteNonQuery();
        IDataReader ExecuteReader();

        NpgsqlParameter ParametersAddWithValue(string name, NpgsqlDbType type, object value);
    }
}
