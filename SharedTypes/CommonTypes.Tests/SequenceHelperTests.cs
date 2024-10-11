using System;
using System.Collections.Generic;
using Xunit;
using CommonTypes;

namespace CommonTypes.Tests
{
    public class SequenceHelperTests
    {
        [Fact]
        public void GetSequence_CompleteSequence_ReturnsCompleteSequence()
        {
            // Arrange
            long? lastProcessedSequenceId = 0;
            var orderedIds = (IOrderedEnumerable<long>)new List<long> { 1, 2, 3, 4, 5 };

            // Act
            var result = SequenceHelper.GetSequence(lastProcessedSequenceId, orderedIds);

            // Assert
            Assert.Equal(lastProcessedSequenceId, result.LastProcessed);
            Assert.Equal(1, result.Min);
            Assert.Equal(5, result.Max);
            Assert.Equal(5, result.Count);
            Assert.Equal(5, result.List.Count);
            Assert.Equal(-1, result.LastInOrder);
            Assert.Equal(-1, result.ExpectedNext);
            Assert.Equal(-1, result.ActualNext);
            Assert.True(result.IsComplete);
        }

        [Fact]
        public void GetSequence_IncompleteSequence_ReturnsIncompleteSequence()
        {
            // Arrange
            long? lastProcessedSequenceId = 0;
            var orderedIds = (IOrderedEnumerable<long>)new List<long> { 1, 2, 3, 5, 6 };

            // Act
            var result = SequenceHelper.GetSequence(lastProcessedSequenceId, orderedIds);

            // Assert
            Assert.Equal(lastProcessedSequenceId, result.LastProcessed);
            Assert.Equal(1, result.Min);
            Assert.Equal(6, result.Max);
            Assert.Equal(5, result.Count);
            Assert.Equal(3, result.List.Count);
            Assert.Equal(3, result.LastInOrder);
            Assert.Equal(4, result.ExpectedNext);
            Assert.Equal(5, result.ActualNext);
            Assert.False(result.IsComplete);
        }

        [Fact]
        public void GetSequence_StartingFromNonZeroLastProcessed_ReturnsCorrectSequence()
        {
            // Arrange
            long? lastProcessedSequenceId = 5;
            var orderedIds = (IOrderedEnumerable<long>)new List<long> { 6, 7, 8, 9, 10 };

            // Act
            var result = SequenceHelper.GetSequence(lastProcessedSequenceId, orderedIds);

            // Assert
            Assert.Equal(lastProcessedSequenceId, result.LastProcessed);
            Assert.Equal(6, result.Min);
            Assert.Equal(10, result.Max);
            Assert.Equal(5, result.Count);
            Assert.Equal(5, result.List.Count);
            Assert.Equal(-1, result.LastInOrder);
            Assert.Equal(-1, result.ExpectedNext);
            Assert.Equal(-1, result.ActualNext);
            Assert.True(result.IsComplete);
        }

        [Fact]
        public void GetSequence_FirstMessageSequenceNotOk_ReturnsEmptyList()
        {
            // Arrange
            long? lastProcessedSequenceId = 5;
            var orderedIds = (IOrderedEnumerable<long>)new List<long> { 7, 8, 9, 10 };

            // Act
            var result = SequenceHelper.GetSequence(lastProcessedSequenceId, orderedIds);

            // Assert
            Assert.Equal(lastProcessedSequenceId, result.LastProcessed);
            Assert.Equal(7, result.Min);
            Assert.Equal(10, result.Max);
            Assert.Equal(4, result.Count);
            Assert.Empty(result.List);
            Assert.Equal(-1, result.LastInOrder);
            Assert.Equal(-1, result.ExpectedNext);
            Assert.Equal(-1, result.ActualNext);
            Assert.False(result.IsComplete);
        }

        [Fact]
        public void GetSequence_EmptyOrderedIds_ReturnsEmptySequence()
        {
            // Arrange
            long? lastProcessedSequenceId = 5;
            var orderedIds = (IOrderedEnumerable<long>)new List<long>();

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => SequenceHelper.GetSequence(lastProcessedSequenceId, orderedIds));
        }

        [Fact]
        public void GetSequence_NullLastProcessedId_HandlesCorrectly()
        {
            // Arrange
            long? lastProcessedSequenceId = null;
            var orderedIds = (IOrderedEnumerable<long>)new List<long> { 1, 2, 3, 4, 5 };

            // Act
            var result = SequenceHelper.GetSequence(lastProcessedSequenceId, orderedIds);

            // Assert
            Assert.Null(result.LastProcessed);
            Assert.Equal(1, result.Min);
            Assert.Equal(5, result.Max);
            Assert.Equal(5, result.Count);
            Assert.Equal(5, result.List.Count);
            Assert.Equal(-1, result.LastInOrder);
            Assert.Equal(-1, result.ExpectedNext);
            Assert.Equal(-1, result.ActualNext);
            Assert.True(result.IsComplete);
        }
    }
}