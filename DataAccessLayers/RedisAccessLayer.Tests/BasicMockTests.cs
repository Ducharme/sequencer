using log4net;
using Moq;
using StackExchange.Redis;

namespace RedisAccessLayer.Tests
{
    public abstract class BasicMockTests
    {
        protected readonly Mock<IRedisConfigurationFetcher> _configFetcherMock;
        protected readonly Mock<IDatabase> _databaseMock;
        protected readonly Mock<IConnectionMultiplexer> _connectionMultiplexerMock;
        protected readonly Mock<ISubscriber> _subscriberMock;
        protected readonly IRedisConnectionManager _connectionManager;
        protected const string ClientNamePrefix = "test-client";

        public BasicMockTests()
        {
            ILog logger = LogManager.GetLogger(typeof(BasicMockTests));

            // Create mock objects
            var behavior = MockBehavior.Strict;
            _configFetcherMock = new Mock<IRedisConfigurationFetcher>(behavior);
            _connectionMultiplexerMock = new Mock<IConnectionMultiplexer>(behavior);
            _databaseMock = new Mock<IDatabase>(behavior);
            _subscriberMock = new Mock<ISubscriber>(behavior);
            var connectionMultiplexerWrapperMock = new Mock<IConnectionMultiplexerWrapper>(behavior);
            var configurationOptionsWrapper = new Mock<IConfigurationOptionsWrapper>(behavior);
            
            // Set up behavior
            _configFetcherMock.SetupGet(cf => cf.ClientName).Returns(ClientNamePrefix);
            _configFetcherMock.SetupGet(cf => cf.OptionsWrapper).Returns(configurationOptionsWrapper.Object);
            connectionMultiplexerWrapperMock.Setup(wrapper => wrapper.Connect(configurationOptionsWrapper.Object, logger)).Returns(_connectionMultiplexerMock.Object);

            _connectionMultiplexerMock.Setup(m => m.GetDatabase(It.IsAny<int>(), null)).Returns(_databaseMock.Object);
            _connectionMultiplexerMock.Setup(m => m.GetSubscriber(null)).Returns(_subscriberMock.Object);
            _connectionMultiplexerMock.SetupGet(m => m.IsConnected).Returns(true);
            _connectionMultiplexerMock.SetupGet(m => m.ClientName).Returns(ClientNamePrefix);

            _connectionManager = new RedisConnectionManager(_configFetcherMock.Object, connectionMultiplexerWrapperMock.Object);
        }
    }
}
