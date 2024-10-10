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
        public static Sequence GetSequence(long? lastProcessedSequenceId, List<long> orderedIds)
        {
            var sequence = new Sequence()
            {
                LastProcessed = lastProcessedSequenceId,
                Min = orderedIds.First(),
                Max = orderedIds.Last(),
                Count = orderedIds.Count,
                List = new List<long>(),
                LastInOrder = -1,
                ExpectedNext = -1,
                ActualNext = -1,
                IsComplete = false
            };
            
            var firstMessageOk = (lastProcessedSequenceId ?? 0) + 1 == orderedIds[0];
            if (firstMessageOk)
            {
                sequence.List.Add(orderedIds[0]);
                for (var i=1; i < orderedIds.Count; i++)
                {
                    var current = orderedIds[i-1];
                    var expectedNext = current + 1;
                    var next = orderedIds[i];
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
                sequence.ActualNext = orderedIds[0];
            }
            sequence.IsComplete = orderedIds.Count == sequence.List.Count;
            return sequence;
         }
    }
}
