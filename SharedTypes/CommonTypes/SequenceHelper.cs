namespace CommonTypes
{
    public struct Sequence
    {
        public long? LastProcessed {get; set;}
        public long Min {get; set;}
        public long Max {get; set;}
        public long Count {get; set;}
        public long LastInOrder {get; set;}
        public long ExpectedNext {get; set;}
        public long ActualNext {get; set;}
        public List<long> List {get; set;}
        public bool IsComplete {get; set;}
    }

    public static class SequenceHelper
    {
        public static Sequence GetSequence(long? lastProcessedSequenceId, IOrderedEnumerable<long> orderedIds)
        {
            var sequence = new Sequence()
            {
                LastProcessed = lastProcessedSequenceId,
                Min = orderedIds.First(),
                Max = orderedIds.Last(),
                Count = orderedIds.Count(),
                List = new List<long>(),
                LastInOrder = -1,
                ExpectedNext = -1,
                ActualNext = -1,
                IsComplete = false
            };
            
            var firstValue = orderedIds.First();
            var firstMessageOk = (lastProcessedSequenceId ?? 0) + 1 == firstValue;
            if (firstMessageOk)
            {
                sequence.List.Add(firstValue);
                for (var i=1; i < orderedIds.Count(); i++)
                {
                    var current = orderedIds.ElementAt(i-1);
                    var expectedNext = current + 1;
                    var next = orderedIds.ElementAt(i);
                    if (expectedNext != next)
                    {
                        sequence.LastInOrder = current;
                        sequence.ExpectedNext = expectedNext;
                        sequence.ActualNext = next;
                        break;
                    } else {
                        sequence.List.Add(next);
                    }
                }
            }
            else
            {
                sequence.LastInOrder = lastProcessedSequenceId ?? -1;
                sequence.ExpectedNext = (lastProcessedSequenceId ?? 0) + 1;
                sequence.ActualNext = firstValue;
            }
            sequence.IsComplete = orderedIds.Count() == sequence.List.Count;
            return sequence;
         }
    }
}
