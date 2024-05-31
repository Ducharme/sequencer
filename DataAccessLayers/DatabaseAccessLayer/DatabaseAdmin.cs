using CommonTypes;
using log4net;

namespace DatabaseAccessLayer
{
    public class DatabaseAdmin: DatabaseClient, IDatabaseAdmin
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(DatabaseAdmin));

        public DatabaseAdmin(IDatabaseConnectionFetcher cf)
            : base (cf)
        {
        }

        public List<MyMessage> GetAllMessages(string name)
        {
            var sql = $"SELECT * FROM {QueryHelper.TableName} WHERE {QueryHelper.ColumnName} = '{name}'";
            return ExecuteQueryAndGetResults(sql);
        }

        public int PurgeTable()
        {
            const string sql = $"DELETE FROM \"{QueryHelper.TableName}\";";
            return ExecuteNonQuery(sql);
        }

        public int DropTable()
        {
            const string sql = $"DROP TABLE IF EXISTS \"{QueryHelper.TableName}\";";
            return ExecuteNonQuery(sql);
        }

        public int CreateTable()
        {
            const string sql = @$"CREATE TABLE {QueryHelper.TableName} (
                id SERIAL PRIMARY KEY,
                {QueryHelper.ColumnName} VARCHAR(20) NOT NULL,
                {QueryHelper.ColumnSequence} INTEGER NOT NULL,
                {QueryHelper.ColumnPayload} VARCHAR(2048) NOT NULL,
                {QueryHelper.ColumnDelay} INTEGER NOT NULL,
                {QueryHelper.ColumnCreatedAt} TIMESTAMP without time zone NOT NULL,
                {QueryHelper.ColumnProcessingAt} TIMESTAMP without time zone NOT NULL,
                {QueryHelper.ColumnProcessedAt} TIMESTAMP without time zone NOT NULL,
                {QueryHelper.ColumnSequencingAt} TIMESTAMP without time zone NOT NULL,
                {QueryHelper.ColumnSavedAt} TIMESTAMP without time zone NOT NULL,
                {QueryHelper.ColumnSequencedAt} TIMESTAMP without time zone
            );";
            return ExecuteNonQuery(sql);
        }
    }
}


