namespace CommonTypes
{
    public static class MyMessageLowerCaseFieldNames
    {
        public static string Sequence = MyMessageFieldNames.Sequence.ToLower();
        public static string Name = MyMessageFieldNames.Name.ToLower();
        public static string Payload = MyMessageFieldNames.Payload.ToLower();
        public static string Delay = MyMessageFieldNames.Delay.ToLower();

        public static string CreatedAt = MyMessageFieldNames.CreatedAt.ToLower();
        public static string ProcessingAt = MyMessageFieldNames.ProcessingAt.ToLower();
        public static string ProcessedAt = MyMessageFieldNames.ProcessedAt.ToLower();
        public static string SequencingAt = MyMessageFieldNames.SequencingAt.ToLower();
        public static string SavedAt = MyMessageFieldNames.SavedAt.ToLower();
        public static string SequencedAt = MyMessageFieldNames.SequencedAt.ToLower();
    }
}