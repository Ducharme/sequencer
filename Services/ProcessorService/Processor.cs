
using CommonTypes;
using RedisAccessLayer;
using DatabaseAccessLayer;

using log4net;

namespace ProcessorService
{
    public interface IProcessor
    {
        Task ReceiveMessageAsync();
    }

    public class Processor : IProcessor
    {
        private readonly IDatabaseClient database_client;
        private readonly IPendingToProcessedListener listener;
        private static readonly ILog logger = LogManager.GetLogger(typeof(Processor));

        public Processor(IDatabaseClient dbc, IPendingToProcessedListener ppl)
        {
            database_client = dbc ?? throw new NullReferenceException("IDatabaseClient implementation could not be resolved");
            listener = ppl ?? throw new NullReferenceException("IPendingToProcessedListener implementation could not be resolved");
        }

        public async Task ReceiveMessageAsync()
        {
            await listener.ListenMessagesFromPendingList(MessageReceivedHandler);
        }

        private async Task<bool> MessageReceivedHandler(string listKey, string message, MyMessage mm)
        {
            logger.Info($"Message received: {message}");

            if (!string.IsNullOrEmpty(mm.Name))
            {
                // Using idempotency in Redis instead of database for performance reasons
                var canBeProcessed = await listener.CanMessageBeProcessed(mm.Name, mm.Sequence); // TODO: Include in ListRightPopLeftPushListSetByIndexInTransactionAsync script
                if (canBeProcessed)
                {
                    logger.Debug($"Processing of sequence id {mm.Sequence} started");
                    if (mm.Delay > 0)
                    {
                        await Task.Delay(mm.Delay); // NOTE: Real work would be done here
                    }
                    mm.ProcessedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    logger.Debug($"Processing of sequence id {mm.Sequence} finished");
                    await listener.AppendToProcessedStreamThenDeleteFromProcessingList(mm, message);
                }
                else
                {
                    logger.Warn($"Message {message} was already processed");
                }
            }
            else
            {
                logger.Error($"Invalid database manager for {message}");
            }
            return true;
        }
    }
}
