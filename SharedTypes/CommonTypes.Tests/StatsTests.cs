using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace CommonTypes.Tests
{
    public class StatsTests
    {
        [Fact]
        public void Constructor_WithValidMessages_ShouldCalculateStatsCorrectly()
        {
            // Arrange
            var messages = new List<MyMessage>
            {
                new MyMessage { CreatedAt = 1000, ProcessingAt = 1100, ProcessedAt = 1200, SequencingAt = 1300, SavedAt = 1400, SequencedAt = 1500, Sequence = 1 },
                new MyMessage { CreatedAt = 2000, ProcessingAt = 2200, ProcessedAt = 2400, SequencingAt = 2600, SavedAt = 2800, SequencedAt = 3000, Sequence = 2 },
                new MyMessage { CreatedAt = 3000, ProcessingAt = 3300, ProcessedAt = 3600, SequencingAt = 3900, SavedAt = 4200, SequencedAt = 4500, Sequence = 3 }
            };

            // Act
            var stats = new Stats(messages);

            // Assert
            Assert.Equal(100, stats.CreatedToProcessingStats["min"]);
            Assert.Equal(300, stats.CreatedToProcessingStats["max"]);
            Assert.Equal(200, stats.CreatedToProcessingStats["avg"]);

            Assert.Equal(100, stats.ProcessingToProcessedStats["min"]);
            Assert.Equal(300, stats.ProcessingToProcessedStats["max"]);
            Assert.Equal(200, stats.ProcessingToProcessedStats["avg"]);

            Assert.Equal(1500, stats.CreatedToSequencedStats["max"]);
            Assert.Equal(500, stats.CreatedToSequencedStats["min"]);
            Assert.Equal(1000, stats.CreatedToSequencedStats["avg"]);
        }

        [Fact]
        public void Constructor_WithEmptyMessages_ShouldHandleGracefully()
        {
            // Arrange
            var messages = new List<MyMessage>();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new Stats(messages));
        }

        [Fact]
        public void GetStats_WithValidValues_ShouldCalculateCorrectly()
        {
            // Arrange
            var values = new List<long> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

            // Act
            var result = Stats.GetStats(values);

            // Assert
            Assert.Equal(5.5, result["50p"]);
            Assert.Equal(9.1, result["90p"]);
            Assert.Equal(9.55, result["95p"]);
            Assert.Equal(9.91, result["99p"]);
            Assert.Equal(5.5, result["avg"]);
            Assert.Equal(1, result["min"]);
            Assert.Equal(10, result["max"]);
        }

        [Fact]
        public void CalculatePercentile_WithValidValues_ShouldCalculateCorrectly()
        {
            // Arrange
            var values = new List<long> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

            // Act & Assert
            Assert.Equal(5.5, Stats.CalculatePercentile(values, 0.5));
            Assert.Equal(9.1, Stats.CalculatePercentile(values, 0.9));
            Assert.Equal(1, Stats.CalculatePercentile(values, 0));
            Assert.Equal(10, Stats.CalculatePercentile(values, 1));
        }

        [Fact]
        public void CalculatePercentile_WithEmptyList_ShouldThrowArgumentException()
        {
            // Arrange
            var values = new List<long>();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => Stats.CalculatePercentile(values, 0.5));
        }

        [Fact]
        public void Constructor_WithSingleMessage_ShouldCalculateStatsCorrectly()
        {
            // Arrange
            var messages = new List<MyMessage>
            {
                new MyMessage { CreatedAt = 1000, ProcessingAt = 1100, ProcessedAt = 1200, SequencingAt = 1300, SavedAt = 1400, SequencedAt = 1500, Sequence = 1 }
            };

            // Act
            var stats = new Stats(messages);

            // Assert
            Assert.Equal(100, stats.CreatedToProcessingStats["min"]);
            Assert.Equal(100, stats.CreatedToProcessingStats["max"]);
            Assert.Equal(100, stats.CreatedToProcessingStats["avg"]);

            Assert.Equal(500, stats.CreatedToSequencedStats["min"]);
            Assert.Equal(500, stats.CreatedToSequencedStats["max"]);
            Assert.Equal(500, stats.CreatedToSequencedStats["avg"]);
        }
    }
}