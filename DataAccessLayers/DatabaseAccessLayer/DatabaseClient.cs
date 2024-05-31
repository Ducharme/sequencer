using CommonTypes;
using DatabaseAL;
using log4net;

namespace DatabaseAccessLayer
{
    public class DatabaseClient : IDatabaseClient, IDisposable
    {
        protected readonly IDatabaseConnection connection;
        protected readonly string connectionString;
        protected bool isConnected = false;

        private static readonly ILog logger = LogManager.GetLogger(typeof(DatabaseClient));

        public DatabaseClient(IDatabaseConnectionFetcher cf)
        {
            this.connection = cf.GetNewConnection();
            this.connectionString = cf.ToString() ?? string.Empty;
            Connect();
        }

        private bool Connect()
        {
            try
            {
                if (!isConnected)
                {
                    this.connection.Open();
                    isConnected = true;
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to connect with {this.connectionString}", ex);
                isConnected = false;
            }
            return isConnected;
        }

        public bool CanMessageBeProcessed(string groupName, long sequence)
        {
            if (!Connect())
            {
                return false;
            }

            try
            {
                var sql = $"SELECT COUNT(*) FROM {QueryHelper.TableName} WHERE {QueryHelper.ColumnName} = '{groupName}' AND {QueryHelper.ColumnSequence} = {sequence}";
                long count = ExecuteScalar(sql);
                return count <= 0;
            }
            catch (Exception ex)
            {
                logger.Error("Failed to execute CanMessageBeProcessed", ex);
                isConnected = false;
                return false;
            }
        }

        public DateTime InsertMessages(List<MyMessage> mms)
        {
            if (!Connect())
            {
                return DateTime.MinValue;
            }

            var insertValuesList = new List<string>();
            for (var i=0; i < mms.Count; i++)
            {
                insertValuesList.Add(QueryHelper.insertParamsTemplate.Replace(QueryHelper.Underscore, QueryHelper.Underscore + i));
            }
            var insertValues = string.Join(",\n", insertValuesList);

            var sql = QueryHelper.GetBulkInsertSql(insertValues);
            
            var command = connection.CreateCommand(sql);
            for (var i=0; i < mms.Count; i++)
            {
                var mm = mms[i];
                command.ParametersAddWithValue(QueryHelper.ParamName + i, NpgsqlTypes.NpgsqlDbType.Varchar, mm.Name ?? string.Empty);
                command.ParametersAddWithValue(QueryHelper.ParamSequence + i, NpgsqlTypes.NpgsqlDbType.Bigint, mm.Sequence);
                command.ParametersAddWithValue(QueryHelper.ParamPayload + i, NpgsqlTypes.NpgsqlDbType.Varchar, mm.Payload ?? string.Empty);
                command.ParametersAddWithValue(QueryHelper.ParamDelay + i, NpgsqlTypes.NpgsqlDbType.Integer, mm.Delay);
                command.ParametersAddWithValue(QueryHelper.ParamCreatedAt + i, NpgsqlTypes.NpgsqlDbType.TimestampTz, DateTimeHelper.GetDateTime(mm.CreatedAt));
                command.ParametersAddWithValue(QueryHelper.ParamProcessingAt + i, NpgsqlTypes.NpgsqlDbType.TimestampTz, DateTimeHelper.GetDateTime(mm.ProcessingAt));
                command.ParametersAddWithValue(QueryHelper.ParamProcessedAt + i, NpgsqlTypes.NpgsqlDbType.TimestampTz, DateTimeHelper.GetDateTime(mm.ProcessedAt));
                command.ParametersAddWithValue(QueryHelper.ParamSequencingAt + i, NpgsqlTypes.NpgsqlDbType.TimestampTz, DateTimeHelper.GetDateTime(mm.SequencingAt));
            }

            object obj;
            long before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            try
            {
                obj = command.ExecuteScalar() ?? DateTime.Now;
            }
            catch (Exception ex)
            {
                logger.Error("Failed to execute InsertMessages", ex);
                obj = DateTime.MinValue;
                isConnected = false;
            }
            long after = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var savedAt = DateTimeHelper.GetDateTimeFromObject(obj);
            if (DateTime.Compare(savedAt, DateTimeHelper.DateTimeMin) == 0)
            {
                var diff = after - before;
                var halfDiff = (long)(diff / 2);
                var timestamp = halfDiff + before;
                savedAt = DateTimeHelper.GetDateTime(timestamp);
            } else {
                var utcOffset = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now);
                savedAt.Add(utcOffset);
            }
            return savedAt;
        }

        protected int ExecuteNonQuery(string sql)
        {
            var command = connection.CreateCommand(sql);
            return command.ExecuteNonQuery();
        }

        protected long ExecuteScalar(string sql)
        {
            var command = connection.CreateCommand(sql);
            var val = command.ExecuteScalar();
            return val == null ? -1 : (long)val;
        }

        protected int ExecuteQueryAndLogResults(string sql)
        {
            var command = connection.CreateCommand(sql);
            var reader = command.ExecuteReader();

            int count = 0;
            while (reader.Read())
            {
                for (int i=0; i < reader.FieldCount; i++)
                {
                    string columnName = reader.GetName(i);
                    object columnValue = reader.GetValue(i);
                    Type columnType = reader.GetFieldType(i);
                    logger.Debug($"{columnName}: {columnValue} [{columnType}]");
                }
                count++;
            }
            return count;
        }

        protected List<MyMessage> ExecuteQueryAndGetResults(string sql)
        {
            var command = connection.CreateCommand(sql);
            var reader = command.ExecuteReader();

            var lst = new List<MyMessage>();
            while (reader.Read())
            {
                var mm = new MyMessage();
                for (int i=0; i < reader.FieldCount; i++)
                {
                    string cn = reader.GetName(i);
                    var columnName = cn.ToLower();
                    object val = reader.GetValue(i);
                    Type columnType = reader.GetFieldType(i);
                    logger.Debug($"columnName:{columnName} columnType:{columnType} val:{val}");

                    if (columnName == MyMessageLowerCaseFieldNames.Name) {
                        mm.Name = val == null ? string.Empty : val.ToString();
                    } else if (columnName == MyMessageLowerCaseFieldNames.Sequence) {
                        mm.Sequence = (long)val;
                    } else if (columnName == MyMessageLowerCaseFieldNames.Payload) {
                        mm.Payload = val == null ? string.Empty : val.ToString();
                    } else if (columnName == MyMessageLowerCaseFieldNames.Delay) {
                        mm.Delay = (int)val;
                    } else if (columnName == MyMessageLowerCaseFieldNames.CreatedAt) {
                        mm.CreatedAt = DateTimeHelper.GetEpochMilliseconds(val);
                    } else if (columnName == MyMessageLowerCaseFieldNames.ProcessingAt) {
                        mm.ProcessingAt = DateTimeHelper.GetEpochMilliseconds(val);
                    } else if (columnName == MyMessageLowerCaseFieldNames.ProcessedAt) {
                        mm.ProcessedAt = DateTimeHelper.GetEpochMilliseconds(val);
                    } else if (columnName == MyMessageLowerCaseFieldNames.SavedAt) {
                        mm.SavedAt = DateTimeHelper.GetEpochMilliseconds(val);
                    } else if (columnName == MyMessageLowerCaseFieldNames.SequencingAt) {
                        mm.SequencingAt = DateTimeHelper.GetEpochMilliseconds(val);
                    } else if (columnName == MyMessageLowerCaseFieldNames.SequencedAt) {
                        mm.SequencedAt = DateTimeHelper.GetEpochMilliseconds(val);
                    } 
                }
                lst.Add(mm);
            }
            return lst;
        }

        public void Dispose()
        {
            this.connection.Dispose();
        }
    }
}
