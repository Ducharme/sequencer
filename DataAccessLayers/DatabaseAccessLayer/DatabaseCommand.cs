using Npgsql;
using NpgsqlTypes;

namespace DatabaseAL
{
    public class DatabaseCommand : IDatabaseCommand
    {
        private readonly NpgsqlCommand command;

        public DatabaseCommand(string commandText, IDatabaseConnection dc)
        {
            command = new NpgsqlCommand(commandText, dc.Connection);
        }

        public object? ExecuteScalar()
        {
            using (command)
            {
                return command.ExecuteScalar();
            }
        }

        public int ExecuteNonQuery()
        {
            using (command)
            {
                return command.ExecuteNonQuery();
            }
        }

        public IDataReader ExecuteReader()
        {
            using (command)
            {
                var er = command.ExecuteReader();
                var dr = new DataReader(er);
                return dr;
            }
        }

        public NpgsqlParameter ParametersAddWithValue(string name, NpgsqlDbType type, object value)
        {
            return command.Parameters.AddWithValue(name, type, value);
        }
    }
}
