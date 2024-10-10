namespace CommonTypes
{
    [Serializable]
    public class OrderingCheck
    {
        public long FirstSeq { get; }
        public long LastSeq { get; }
        public bool IsOrdered { get; }
        public long? BrokenAfter { get; }
        public long? BrokenSeq { get; }
        public long[] Others { get; }

        public OrderingCheck(List<MyMessage> mms)
        {
            var lst = new List<MyMessage>(mms);
            FirstSeq = lst.First().Sequence;
            LastSeq = lst.Last().Sequence;
            IsOrdered = true;
            var others = new List<long>();

            long lastItemSeq = -1;
            foreach (var mm in lst)
            {
                if (IsOrdered)
                {
                    if (lastItemSeq < 0)
                    {
                        lastItemSeq = mm.Sequence;
                    }
                    else
                    {
                        var expected = lastItemSeq + 1;
                        if (mm.Sequence != expected)
                        {
                            IsOrdered = false;
                            BrokenAfter = lastItemSeq;
                            BrokenSeq = mm.Sequence;
                        }
                        else
                        {
                            lastItemSeq = mm.Sequence;
                        }
                    }
                }
                
                if (!IsOrdered)
                {
                    others.Add(mm.Sequence);
                }
            }

            Others = others.ToArray();
        }
    }
}