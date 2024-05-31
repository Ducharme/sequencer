
namespace DatabaseAL
{
    public interface IDataReader
    {
        bool Read();
        int FieldCount { get; }
        string GetName(int ordinal);
        object GetValue(int ordinal);
        Type GetFieldType(int ordinal);
    }
}