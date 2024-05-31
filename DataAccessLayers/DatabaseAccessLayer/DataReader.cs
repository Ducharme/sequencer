using Npgsql;

namespace DatabaseAL
{
    public class DataReader : IDataReader
    {
        private NpgsqlDataReader reader;

        public DataReader(NpgsqlDataReader ndr)
        {
            reader = ndr;
        }

        public int FieldCount => reader.FieldCount;

        public bool Read()
        {
            return reader.Read();
        }

        public string GetName(int ordinal)
        {
            return reader.GetName(ordinal);
        }

        public object GetValue(int ordinal)
        {
            return reader.GetValue(ordinal);
        }

        public Type GetFieldType(int ordinal)
        {
            return reader.GetFieldType(ordinal);
        }
    }
}