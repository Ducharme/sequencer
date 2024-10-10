
using System.Text.Json;

namespace CommonTypes
{
    public class MyMessage
    {
        public long Sequence {get; set;}
        public string? Name {get; set;}
        public string? Payload {get; set;}
        public int Delay {get; set;}

        public long CreatedAt {get; set;} = 0;
        public long ProcessingAt {get; set;} = 0;
        public long ProcessedAt {get; set;} = 0;
        public long SequencingAt {get; set;} = 0;
        public long SavedAt {get; set;} = 0;
        public long SequencedAt {get; set;} = 0;

        protected const string DateTimeFormat = "yyyy/MM/dd HH:mm:ss.fff";

        public static string GetDateTimeAsString(long timestamp)
        {
            var dt = DateTimeHelper.GetDateTime(timestamp);
            return dt.ToString(DateTimeFormat);
        }

        public static string GetTimeAsString(long timestamp)
        {
            var dt = DateTimeHelper.GetDateTime(timestamp);
            return $"{dt.Hour}:{dt.Minute}:{dt.Second}.{dt.Millisecond}";
        }

        public override string ToString()
        {
            return ToShortString();
        }

        public virtual string ToLongString()
        {
            return $"Sequence:{Sequence}, Name:{Name}, Payload:{Payload}, Delay:{Delay}, CreatedAt:{CreatedAt}, ProcessingAt:{ProcessingAt}, ProcessedAt:{ProcessedAt}, SequencingAt:{SequencingAt}, SavedAt:{SavedAt}, SequencedAt:{SequencedAt}";
        }

        public virtual string ToShortString()
        {
            // Example: 1;poc;a;500;1715230115432;1715230115638;1715230116138;1715230116181;1715230116427;1715230116953
            return $"{Sequence};{Name};{Payload};{Delay};{CreatedAt};{ProcessingAt};{ProcessedAt};{SequencingAt};{SavedAt};{SequencedAt}";
        }

        public virtual string ToDateString()
        {
            return $"Sequence:{Sequence}, Name:{Name}, Payload:{Payload}, Delay:{Delay}, CreatedAt:{GetDateTimeAsString(CreatedAt)}, ProcessingAt:{GetDateTimeAsString(ProcessingAt)}, ProcessedAt:{GetDateTimeAsString(ProcessedAt)}, SequencingAt:{GetDateTimeAsString(SequencingAt)}, SavedAt:{GetDateTimeAsString(SavedAt)}, SequencedAt:{GetDateTimeAsString(SequencedAt)}";
        }

        public virtual string ToTimeString()
        {
            return $"Sequence:{Sequence}, Name:{Name}, Payload:{Payload}, Delay:{Delay}, CreatedAt:{GetTimeAsString(CreatedAt)}, ProcessingAt:{GetTimeAsString(ProcessingAt)}, ProcessedAt:{GetTimeAsString(ProcessedAt)}, SequencingAt:{GetTimeAsString(SequencingAt)}, SavedAt:{GetTimeAsString(SavedAt)}, SequencedAt:{GetTimeAsString(SequencedAt)}";
        }

        public virtual string ToTinyString()
        {
            return $"{Sequence};{Name};{Payload};{Delay}";
        }

        public virtual string ToJson()
        {
            return $"{{\"Sequence\":{Sequence},\"Name\":\"{Name}\",\"Payload\":\"{Payload}\",\"Delay\":{Delay},\"CreatedAt\":{CreatedAt},\"ProcessingAt\":{ProcessingAt},\"ProcessedAt\":{ProcessedAt},\"SequencingAt\":{SequencingAt},\"SavedAt\":{SavedAt},\"SequencedAt\":{SequencedAt}}}";
        }

        public virtual object ToJson2()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}