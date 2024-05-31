using System.Text.Json;

namespace CommonTypes
{
    public static class MyMessageExtensions
    {
        public static string ToJson(this MyMessage mm) => JsonSerializer.Serialize(mm);

        public static MyMessage? FromJson(this string json) => JsonSerializer.Deserialize<MyMessage>(json);

        public static MyMessage? FromShortString(this string str)
        {
            var tokens = str.Split(';');
            return new MyMessage
            {
                Sequence = long.Parse(tokens[0]),
                Name = tokens[1],
                Payload = tokens[2],
                Delay = int.Parse(tokens[3]),
                CreatedAt = long.TryParse(tokens[4], out long tmp4) ? tmp4 : 0,
                ProcessingAt = long.TryParse(tokens[5], out long tmp5) ? tmp5 : 0,
                ProcessedAt = long.TryParse(tokens[6], out long tmp6) ? tmp6 : 0,
                SequencingAt = long.TryParse(tokens[7], out long tmp7) ? tmp7 : 0,
                SavedAt = long.TryParse(tokens[8], out long tmp8) ? tmp8 : 0,
                SequencedAt = long.TryParse(tokens[9], out long tmp9) ? tmp9 : 0
            };
        }
    }
}