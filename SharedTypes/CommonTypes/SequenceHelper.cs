
using log4net;

namespace CommonTypes
{
    public static class SequenceHelper
    {
         public static List<long> GetPartialSequence(long? lastProcessedSequenceId, List<long> unorderedIds, ILog logger)
         {
            var partialSequence = new List<long>();
            var firstMessageSequenceOk = lastProcessedSequenceId + 1 == unorderedIds[0];
            if (firstMessageSequenceOk)
            {
                partialSequence.Add(unorderedIds[0]);
                for (var i=1; i < unorderedIds.Count; i++)
                {
                    var current = unorderedIds[i-1];
                    var expectedNext = current + 1;
                    var next = unorderedIds[i];
                    if (expectedNext != next)
                    {
                        logger.Debug($"Partial sequence for pending stream ends at {current}, expecting {expectedNext} but next is {next}");
                        break;
                    } else {
                        partialSequence.Add(next);
                    }
                }
            }
            return partialSequence;
         }

        public static bool IsSequenceComplete(long? lastProcessedSequenceId, List<long> unorderedIds, ILog logger)
        {
            var ids = new List<long>(unorderedIds);
            ids.Sort();

            var sequenceComplete = lastProcessedSequenceId + 1 == ids[0];
            if (sequenceComplete)
            {
                for (var i=1; i < ids.Count; i++)
                {
                    var current = ids[i-1];
                    var expectedNext = current + 1;
                    var next = ids[i];
                    if (expectedNext != next)
                    {
                        sequenceComplete = false;
                        logger.Debug($"Sequence of pending stream after {current} is missing, expecting {expectedNext} but next is {next}, lastProcessedSequenceId={lastProcessedSequenceId}");
                        break;
                    }
                }
            }
            return sequenceComplete;
        }
    }
}
