using Moq;
using log4net;

namespace CommonTypes.Tests
    {
    public class PerfsTests
    {
        private readonly Mock<ILog> _mockLogger;

        public PerfsTests()
        {
            _mockLogger = new Mock<ILog>();
        }

        [Fact]
        public async Task Constructor_WithValidMessages_ShouldCalculateRatesCorrectly()
        {
            // Arrange
            var messages = new List<MyMessage>
            {
                new MyMessage { ProcessingAt = 0, SequencingAt = 0 },
                new MyMessage { ProcessingAt = 900, SequencingAt = 1500 },
                new MyMessage { ProcessingAt = 1200, SequencingAt = 1700 },
                new MyMessage { ProcessingAt = 1800, SequencingAt = 2300 },
                new MyMessage { ProcessingAt = 2100, SequencingAt = 2600 }
            };

            // Act
            var perfs = await Perfs.CreateAsync(messages);

            // Assert
            Assert.Equal(3, perfs.ProcessingRatePerSecond.Count);
            Assert.Equal(2, perfs.ProcessingRatePerSecond[0]);
            Assert.Equal(2, perfs.ProcessingRatePerSecond[1]);
            Assert.Equal(1, perfs.ProcessingRatePerSecond[2]);

            Assert.Equal(3, perfs.SequencingRatePerSecond.Count);
            Assert.Equal(1, perfs.SequencingRatePerSecond[0]);
            Assert.Equal(2, perfs.SequencingRatePerSecond[1]);
            Assert.Equal(2, perfs.SequencingRatePerSecond[2]);

            Assert.Equal(1.67, perfs.ProcessingRatePerSecondStats["avg"]);
            Assert.Equal(1, perfs.SequencingRatePerSecondStats["min"]);
            Assert.Equal(2, perfs.SequencingRatePerSecondStats["max"]);
            Assert.Equal(1.9, Math.Round(perfs.AverageRatePerSecond, 1));
        }

        [Fact]
        public async Task Constructor_WithEmptyMessages_ShouldHandleGracefully()
        {
            // Arrange
            var messages = new List<MyMessage>();

            // Act
            var perfs = await Perfs.CreateAsync(messages);

            // Assert
            Assert.Empty(perfs.ProcessingRatePerSecond);
            Assert.Empty(perfs.SequencingRatePerSecond);
            Assert.Empty(perfs.ProcessingRatePerSecondStats);
            Assert.Empty(perfs.SequencingRatePerSecondStats);
            Assert.Equal(0, perfs.AverageRatePerSecond);
        }

        [Fact]
        public async Task Constructor_WithSingleMessage_ShouldCalculateRatesCorrectly()
        {
            // Arrange
            var messages = new List<MyMessage>
            {
                new MyMessage { ProcessingAt = 1000, SequencingAt = 1500 }
            };

            // Act
            var perfs = await Perfs.CreateAsync(messages);

            // Assert
            Assert.Single(perfs.ProcessingRatePerSecond);
            Assert.Equal(1, perfs.ProcessingRatePerSecond[0]);

            Assert.Single(perfs.SequencingRatePerSecond);
            Assert.Equal(1, perfs.SequencingRatePerSecond[0]);

            Assert.Equal(2, Math.Round(perfs.AverageRatePerSecond, 1));
        }

        [Fact]
        public async Task Constructor_WithMessagesSpanningMultipleSeconds_ShouldCalculateRatesCorrectly()
        {
            // Arrange
            var messages = new List<MyMessage>
            {
                new MyMessage { ProcessingAt = 0, SequencingAt = 500 },
                new MyMessage { ProcessingAt = 1000, SequencingAt = 1500 },
                new MyMessage { ProcessingAt = 2000, SequencingAt = 2500 },
                new MyMessage { ProcessingAt = 3000, SequencingAt = 3500 },
                new MyMessage { ProcessingAt = 4000, SequencingAt = 4500 }
            };

            // Act
            var perfs = await Perfs.CreateAsync(messages);

            // Assert
            Assert.Equal(5, perfs.ProcessingRatePerSecond.Count);
            Assert.All(perfs.ProcessingRatePerSecond.Values, v => Assert.Equal(1, v));

            Assert.Equal(5, perfs.SequencingRatePerSecond.Count);
            Assert.All(perfs.SequencingRatePerSecond.Values, v => Assert.Equal(1, v));

            Assert.Equal(1.11, Math.Round(perfs.AverageRatePerSecond, 2));
        }
    }
}