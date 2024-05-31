using DatabaseAL;

namespace DatabaseAccessLayer
{
    public abstract class DatabaseConnectionBaseFetcher(string enpoint, int port = 5432, string username = DatabaseConnectionBaseFetcher.NotFound, string password = DatabaseConnectionBaseFetcher.NotFound, string instance = "sequencer") : IDatabaseConnectionFetcher
    {
        protected const string NotFound = "NotFound";
        protected readonly string Endpoint = enpoint;
        protected readonly int Port = port;
        protected readonly string Username = username;
        protected readonly string Password = password;
        protected readonly string Instance = instance;

        protected string ConnStringWithUserFormat => $"Server={Endpoint};Port={Port};Database={Instance};User Id={Username};Password={Password};";
        protected string ConnStringWithoutUserFormat => $"Server={Endpoint};Port={Port};Database={Instance}";

        public virtual IDatabaseConnection GetNewConnection()
        {
            var format = Username == NotFound && Password == NotFound ? ConnStringWithoutUserFormat : ConnStringWithoutUserFormat;
            return new DatabaseConnection(format);
        }
        
        public override string ToString()
        {
            return $"Server={Endpoint};Port={Port};Database={Instance};User Id={Username};Password=<masked>;";
        }
    }
}