
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CommonTypes
{
    public class MyMessage
    {
        public long Sequence {get; set;}
        public string? Name {get; set;}
        public string? Payload {get; set;}
        public int Delay {get; set;}

        public long CreatedAt {get; set;}
        public long ProcessingAt {get; set;}
        public long ProcessedAt {get; set;}
        public long SequencingAt {get; set;}
        public long SavedAt {get; set;}
        public long SequencedAt {get; set;}

        private const string DateTimeFormat = "yyyy/MM/dd HH:mm:ss.fff";

        private static string GetDateTimeAsString(long timestamp)
        {
            var dt = DateTimeHelper.GetDateTime(timestamp);
            return dt.ToString(DateTimeFormat);
        }

        private static string GetTimeAsString(long timestamp)
        {
            var dt = DateTimeHelper.GetDateTime(timestamp);
            return $"{dt.Hour}:{dt.Minute}:{dt.Second}.{dt.Millisecond}";
        }

        public override string ToString()
        {
            return ToShortString();
        }

        public string ToLongString()
        {
            return $"Sequence:{Sequence}, Name:{Name}, Payload:{Payload}, Delay:{Delay}, CreatedAt:{CreatedAt}, ProcessingAt:{ProcessingAt}, ProcessedAt:{ProcessedAt}, SequencingAt:{SequencingAt}, SavedAt:{SavedAt}, SequencedAt:{SequencedAt}";
        }

        public string ToShortString()
        {
            // Example: 1;poc;a;500;1715230115432;1715230115638;1715230116138;1715230116181;1715230116427;1715230116953
            return $"{Sequence};{Name};{Payload};{Delay};{CreatedAt};{ProcessingAt};{ProcessedAt};{SequencingAt};{SavedAt};{SequencedAt}";
        }

        public string ToDateString()
        {
            return $"Sequence:{Sequence}, Name:{Name}, Payload:{Payload}, Delay:{Delay}, CreatedAt:{GetDateTimeAsString(CreatedAt)}, ProcessingAt:{GetDateTimeAsString(ProcessingAt)}, ProcessedAt:{GetDateTimeAsString(ProcessedAt)}, SequencingAt:{GetDateTimeAsString(SequencingAt)}, SavedAt:{GetDateTimeAsString(SavedAt)}, SequencedAt:{GetDateTimeAsString(SequencedAt)}";
        }

        public string ToTimeString()
        {
            return $"Sequence:{Sequence}, Name:{Name}, Payload:{Payload}, Delay:{Delay}, CreatedAt:{GetTimeAsString(CreatedAt)}, ProcessingAt:{GetTimeAsString(ProcessingAt)}, ProcessedAt:{GetTimeAsString(ProcessedAt)}, SequencingAt:{GetTimeAsString(SequencingAt)}, SavedAt:{GetTimeAsString(SavedAt)}, SequencedAt:{GetTimeAsString(SequencedAt)}";
        }

        public string ToTinyString()
        {
            return $"{Sequence};{Name};{Payload};{Delay}";
        }

        public string ToJson()
        {
            return $"{{\"Sequence\":{Sequence},\"Name\":\"{Name}\",\"Payload\":\"{Payload}\",\"Delay\":{Delay},\"CreatedAt\":{CreatedAt},\"ProcessingAt\":{ProcessingAt},\"ProcessedAt\":{ProcessedAt},\"SequencingAt\":{SequencingAt},\"SavedAt\":{SavedAt},\"SequencedAt\":{SequencedAt}}}";
        }

        public object ToJson2()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}