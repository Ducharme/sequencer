using System;
using System.Collections.Generic;
using Xunit;

namespace CommonTypes.Tests
{
    public class OrderingCheckTests
    {
        [Fact]
        public void Constructor_WithOrderedList_ShouldCreateOrderedCheck()
        {
            var messages = new List<MyMessage>
            {
                new MyMessage { Sequence = 1 },
                new MyMessage { Sequence = 2 },
                new MyMessage { Sequence = 3 },
                new MyMessage { Sequence = 4 },
                new MyMessage { Sequence = 5 }
            };

            var orderingCheck = new OrderingCheck(messages);

            Assert.Equal(1, orderingCheck.FirstSeq);
            Assert.Equal(5, orderingCheck.LastSeq);
            Assert.True(orderingCheck.IsOrdered);
            Assert.Null(orderingCheck.BrokenAfter);
            Assert.Null(orderingCheck.BrokenSeq);
            Assert.Empty(orderingCheck.Others);
        }

        [Fact]
        public void Constructor_WithUnorderedList_ShouldCreateUnorderedCheck()
        {
            var messages = new List<MyMessage>
            {
                new MyMessage { Sequence = 1 },
                new MyMessage { Sequence = 2 },
                new MyMessage { Sequence = 4 },
                new MyMessage { Sequence = 3 },
                new MyMessage { Sequence = 5 }
            };

            var orderingCheck = new OrderingCheck(messages);

            Assert.Equal(1, orderingCheck.FirstSeq);
            Assert.Equal(5, orderingCheck.LastSeq);
            Assert.False(orderingCheck.IsOrdered);
            Assert.Equal(2, orderingCheck.BrokenAfter);
            Assert.Equal(4, orderingCheck.BrokenSeq);
            Assert.Equal(new long[] { 4, 3, 5 }, orderingCheck.Others);
        }

        [Fact]
        public void Constructor_WithSingleMessage_ShouldCreateOrderedCheck()
        {
            var messages = new List<MyMessage>
            {
                new MyMessage { Sequence = 1 }
            };

            var orderingCheck = new OrderingCheck(messages);

            Assert.Equal(1, orderingCheck.FirstSeq);
            Assert.Equal(1, orderingCheck.LastSeq);
            Assert.True(orderingCheck.IsOrdered);
            Assert.Null(orderingCheck.BrokenAfter);
            Assert.Null(orderingCheck.BrokenSeq);
            Assert.Empty(orderingCheck.Others);
        }

        [Fact]
        public void Constructor_WithEmptyList_ShouldThrowException()
        {
            var messages = new List<MyMessage>();

            Assert.Throws<InvalidOperationException>(() => new OrderingCheck(messages));
        }

        [Fact]
        public void Constructor_WithUnorderedListAtStart_ShouldCreateUnorderedCheck()
        {
            var messages = new List<MyMessage>
            {
                new MyMessage { Sequence = 2 },
                new MyMessage { Sequence = 1 },
                new MyMessage { Sequence = 3 },
                new MyMessage { Sequence = 4 },
                new MyMessage { Sequence = 5 }
            };

            var orderingCheck = new OrderingCheck(messages);

            Assert.Equal(2, orderingCheck.FirstSeq);
            Assert.Equal(5, orderingCheck.LastSeq);
            Assert.False(orderingCheck.IsOrdered);
            Assert.Equal(2, orderingCheck.BrokenAfter);
            Assert.Equal(1, orderingCheck.BrokenSeq);
            Assert.Equal(new long[] { 1, 3, 4, 5 }, orderingCheck.Others);
        }

        [Fact]
        public void Constructor_WithUnorderedListAtEnd_ShouldCreateUnorderedCheck()
        {
            var messages = new List<MyMessage>
            {
                new MyMessage { Sequence = 1 },
                new MyMessage { Sequence = 2 },
                new MyMessage { Sequence = 3 },
                new MyMessage { Sequence = 5 },
                new MyMessage { Sequence = 4 }
            };

            var orderingCheck = new OrderingCheck(messages);

            Assert.Equal(1, orderingCheck.FirstSeq);
            Assert.Equal(4, orderingCheck.LastSeq);
            Assert.False(orderingCheck.IsOrdered);
            Assert.Equal(3, orderingCheck.BrokenAfter);
            Assert.Equal(5, orderingCheck.BrokenSeq);
            Assert.Equal(new long[] { 5, 4 }, orderingCheck.Others);
        }
    }
}