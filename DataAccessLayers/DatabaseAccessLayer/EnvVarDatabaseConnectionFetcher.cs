
using CommonTypes;

namespace DatabaseAccessLayer
{
    public class EnvVarDatabaseConnectionFetcher : DatabaseConnectionBaseFetcher
    {
        public EnvVarDatabaseConnectionFetcher()
            : base(EnvVarReader.GetString("PGSQL_ENDPOINT", NotFound),
                EnvVarReader.GetInt("PGSQL_PORT", 5432),
                EnvVarReader.GetString("PGSQL_USERNAME", NotFound),
                EnvVarReader.GetString("PGSQL_PASSWORD", NotFound),
                EnvVarReader.GetString("PGSQL_DATABASE", NotFound))
        {
        }
    }
}