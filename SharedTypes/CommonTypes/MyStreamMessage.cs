
using System.Text.Json;

namespace CommonTypes
{
    public class MyStreamMessage : MyMessage
    {
        public string? StreamId {get; set;}

        public override string ToString()
        {
            return ToShortString();
        }

        public override string ToLongString()
        {
            return base.ToLongString() + $", StreamId: {StreamId}";
        }

        public override string ToShortString()
        {
            return base.ToShortString() + $";{StreamId}";
        }

        public override string ToDateString()
        {
            return base.ToDateString() + $", StreamId:{StreamId}";
        }

        public override string ToTimeString()
        {
            return base.ToTimeString() + $", StreamId:{StreamId}";
        }

        public override string ToTinyString()
        {
            return $"{Sequence};{Name};{Payload};{Delay}";
        }

        public override string ToJson()
        {
            var arr = new char[] { '{', '}' };
            return $"{{ {base.ToJson().Trim(arr)}, \"StreamId\":{StreamId}}}";
        }

        public override object ToJson2()
        {
            return JsonSerializer.Serialize(this);
        }
    }
}