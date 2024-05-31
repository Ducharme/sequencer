using CommonTypes;
using StackExchange.Redis;

namespace RedisAccessLayer
{
    public static class RedisTypeConverter
    {
        public static MyMessage ToMyMessage(this StreamEntry entry)
        {
            var mm = new MyMessage();
            for (var i = 0; i < entry.Values.Length; i++)
            {
                var nve = entry.Values[i];
                switch (nve.Name)
                {
                    case MyMessageFieldNames.Name: mm.Name = nve.Value; break;
                    case MyMessageFieldNames.Sequence: mm.Sequence = (long)nve.Value; break;
                    case MyMessageFieldNames.Payload: mm.Payload = nve.Value; break;
                    case MyMessageFieldNames.Delay: mm.Delay = (int)nve.Value; break;
                    case MyMessageFieldNames.CreatedAt: mm.CreatedAt = (long)nve.Value; break;
                    case MyMessageFieldNames.ProcessingAt: mm.ProcessingAt = (long)nve.Value; break;
                    case MyMessageFieldNames.ProcessedAt: mm.ProcessedAt = (long)nve.Value; break;
                    case MyMessageFieldNames.SavedAt: mm.SavedAt = (long)nve.Value; break;
                    case MyMessageFieldNames.SequencingAt: mm.SequencingAt = (long)nve.Value; break;
                    case MyMessageFieldNames.SequencedAt: mm.SequencedAt = (long)nve.Value; break;
                    default: break;
                }
            }

            return mm;
        }

        public static NameValueEntry[] ToNameValueEntries(this MyMessage mm)
        {
            var name = new NameValueEntry(MyMessageFieldNames.Name, mm.Name);
            var seq = new NameValueEntry(MyMessageFieldNames.Sequence, mm.Sequence);
            var payload = new NameValueEntry(MyMessageFieldNames.Payload, mm.Payload);
            var delay = new NameValueEntry(MyMessageFieldNames.Delay, mm.Delay);
            var createdAt = new NameValueEntry(MyMessageFieldNames.CreatedAt, mm.CreatedAt);
            var processingAt = new NameValueEntry(MyMessageFieldNames.ProcessingAt, mm.ProcessingAt);
            var processedAt = new NameValueEntry(MyMessageFieldNames.ProcessedAt, mm.ProcessedAt);
            var sequencingAt = new NameValueEntry(MyMessageFieldNames.SequencingAt, mm.SequencingAt);
            var savedAt = new NameValueEntry(MyMessageFieldNames.SavedAt, mm.SavedAt);
            var sequencedAt = new NameValueEntry(MyMessageFieldNames.SequencedAt, mm.SequencedAt);
            var nves = new NameValueEntry[] { seq, name, payload, delay, createdAt, processingAt, processedAt, sequencingAt, savedAt, sequencedAt };
            return nves;
        }

        public static NameValueEntry[] ToNameValueEntriesWithExtraString(this MyMessage mm, KeyValuePair<string, string> extra)
        {
            var name = new NameValueEntry(MyMessageFieldNames.Name, mm.Name);
            var seq = new NameValueEntry(MyMessageFieldNames.Sequence, mm.Sequence);
            var payload = new NameValueEntry(MyMessageFieldNames.Payload, mm.Payload);
            var delay = new NameValueEntry(MyMessageFieldNames.Delay, mm.Delay);
            var createdAt = new NameValueEntry(MyMessageFieldNames.CreatedAt, mm.CreatedAt);
            var processingAt = new NameValueEntry(MyMessageFieldNames.ProcessingAt, mm.ProcessingAt);
            var processedAt = new NameValueEntry(MyMessageFieldNames.ProcessedAt, mm.ProcessedAt);
            var sequencingAt = new NameValueEntry(MyMessageFieldNames.SequencingAt, mm.SequencingAt);
            var savedAt = new NameValueEntry(MyMessageFieldNames.SavedAt, mm.SavedAt);
            var sequencedAt = new NameValueEntry(MyMessageFieldNames.SequencedAt, mm.SequencedAt);
            var extraEntry = new NameValueEntry(extra.Key, extra.Value);
            var nves = new NameValueEntry[] { seq, name, payload, delay, createdAt, processingAt, processedAt, sequencingAt, savedAt, sequencedAt, extraEntry };
            return nves;
        }
    }
}

