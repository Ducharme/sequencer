using CommonTypes;
using DatabaseAccessLayer;
using RedisAccessLayer;

using log4net;

namespace SequencerService
{
    public interface ISequencer
    {
        Task ReceiveMessageAsync();
    }

    public class Sequencer : ISequencer
    {
        private IDatabaseClient database_client;
        private IProcessedToSequencedListener listener;
        private static readonly ILog logger = LogManager.GetLogger(typeof(Sequencer));
        private readonly int breakPosition = GetBreakPosition();

        public Sequencer(IDatabaseClient dbc, IProcessedToSequencedListener psl)
        {
            database_client = dbc ?? throw new NullReferenceException("IDatabaseManager implementation could not be resolved");
            listener = psl ?? throw new NullReferenceException("IProcessedToSequencedListener implementation could not be resolved");
        }

        private static int GetBreakPosition ()
        {
            var posEnvVar = Environment.GetEnvironmentVariable("BREAK_POS");
            return int.TryParse(posEnvVar, out int result) ? result : 0;
        }

        private async Task<bool> HandleSequencingMessages(Dictionary<string, MyMessage> dic)
        {
            var orderedBySequence = dic.Select(kvp => kvp.Value).OrderBy(mm => mm.Sequence).ToList();
            if (database_client != null)
            {
                var orderedByEntryId = dic.Select(kvp => kvp.Key).OrderBy(id => id);
                var streamEntryIds = string.Join(",", orderedByEntryId);
                var seqIds = string.Join(",", orderedBySequence.Select(mm => mm.Sequence));
                logger.Info($"Inserting sequencing message streamEntryIds {streamEntryIds} with sequence id {seqIds} to the database");
                
                var dt = database_client.InsertMessages(orderedBySequence);
                var savedAt = DateTimeHelper.GetTimestamp(dt);
                foreach (var mm in orderedBySequence)
                {
                    mm.SavedAt = savedAt;
                }
            } else {
                long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                foreach (var mm in orderedBySequence)
                {
                    mm.SavedAt = timestamp;
                }
            }

            var tuple = await listener.FromProcessedToSequenced(dic);
            return tuple.Item1;
        }

        public async Task ReceiveMessageAsync()
        {
            if (listener != null)
            {
                await listener.ListenForPendingMessages(HandleSequencingMessages);
            }
        }
    }
}
