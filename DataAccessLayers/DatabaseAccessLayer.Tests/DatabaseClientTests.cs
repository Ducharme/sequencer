using DatabaseAL;

using Moq;

namespace DatabaseAccessLayer.Tests
{  
    public class DatabaseClientTests
    {
        private const string ConnectionString = "Server=mock;Port=12345;Database=db;User Id=user;Password=pass;";
        private readonly Mock<IDatabaseConnectionFetcher> connectionFetcherMock;
        private readonly Mock<IDatabaseConnection> connectionMock;
        private const string GroupName = "poc";

        public DatabaseClientTests()
        {
            connectionFetcherMock = new Mock<IDatabaseConnectionFetcher>();
            connectionMock = new Mock<IDatabaseConnection>();
        }

        [Fact]
        public void CanMessageBeProcessed_WhenProcessingForTheFirstTime()
        {
            // Arrange
            var commandMock = new Mock<IDatabaseCommand>();

            connectionMock.Setup(c => c.Open()).Callback(() => { Console.WriteLine("Connected to the database"); });
            connectionMock.Setup(c => c.CreateCommand(It.IsAny<string>())).Returns(commandMock.Object);
            commandMock.Setup(c => c.ExecuteScalar()).Returns((long)0);

            connectionFetcherMock.Setup(c => c.GetNewConnection()).Returns(connectionMock.Object);

            // Act
            var databaseClient = new DatabaseClient(connectionFetcherMock.Object);
            var result = databaseClient.CanMessageBeProcessed(GroupName, 1);

            // Assert
            connectionMock.Verify(c => c.Open(), Times.Once);
            commandMock.Verify(c => c.ExecuteScalar(), Times.Once);
            Assert.True(result);
        }
    }
}