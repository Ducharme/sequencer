namespace DatabaseAccessLayer
{
    internal static class QueryHelper
    {
        public const string TableName = "events";

        private const string At = "@";
        public const string Underscore = "_";

        public const string ColumnName = "name";
        public const string ColumnSequence = "sequence";
        public const string ColumnPayload = "payload";
        public const string ColumnDelay = "delay";
        public const string ColumnCreatedAt = "createdAt";
        public const string ColumnProcessingAt = "processingAt";
        public const string ColumnProcessedAt = "processedAt";
        public const string ColumnSequencingAt = "sequencingAt";
        public const string ColumnSavedAt = "savedAt";
        public const string ColumnSequencedAt = "sequencedAt";


        public static readonly string ParamName = GetParamFromColumn(ColumnName);
        public static readonly string ParamSequence = GetParamFromColumn(ColumnSequence);
        public static readonly string ParamPayload = GetParamFromColumn(ColumnPayload);
        public static readonly string ParamDelay = GetParamFromColumn(ColumnDelay);
        public static readonly string ParamCreatedAt = GetParamFromColumn(ColumnCreatedAt);
        public static readonly string ParamProcessingAt = GetParamFromColumn(ColumnProcessingAt);
        public static readonly string ParamProcessedAt = GetParamFromColumn(ColumnProcessedAt);
        public static readonly string ParamSequencingAt = GetParamFromColumn(ColumnSequencingAt);

        private const string ColumnsPlaceholder = "COLUMNS_PLACEHOLDER";
        private const string ValuesPlaceholder = "VALUES_PLACEHOLDER";

        private static readonly string insertQueryTemplate = $"INSERT INTO events {ColumnsPlaceholder} \nVALUES \n\t{ValuesPlaceholder} \nRETURNING {ColumnSavedAt}";
        private static readonly string insertColumns = $"({ColumnName}, {ColumnSequence}, {ColumnPayload}, {ColumnDelay}, {ColumnCreatedAt}, {ColumnProcessingAt}, {ColumnProcessedAt}, {ColumnSequencingAt}, {ColumnSavedAt})";
        public static readonly string insertParamsTemplate = $"({ParamName}, {ParamSequence}, {ParamPayload}, {ParamDelay}, {ParamCreatedAt}, {ParamProcessingAt}, {ParamProcessedAt}, {ParamSequencingAt}, LOCALTIMESTAMP)";
        

        public static string GetBulkInsertSql(string insertValues)
        {
            return insertQueryTemplate.Replace(ColumnsPlaceholder, insertColumns).Replace(ValuesPlaceholder, insertValues);
        }

        public static string GetParamFromColumn(string column)
        {
            return string.Concat(At, column, Underscore);
        }
    }
}


