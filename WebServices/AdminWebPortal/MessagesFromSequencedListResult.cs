
using AdminService;
using CommonTypes;

public class MessagesFromSequencedListResult
{
    public IResult? Result { get; private set; }
    public List<MyMessage>? Filtered;
    private readonly string name;
    private readonly long start;
    private long count;

    public MessagesFromSequencedListResult(string name, long start, long count)
    {
        this.name = name;
        this.start = start;
        this.count = count;

        if (start < 1)
        {
            Result = Results.BadRequest("start must be 1 or greater");
        }
        else if (count == 0)
        {
            Result = Results.Json(new {});
        }
        else
        {
            Result = null;
        }
    }

    public async Task<List<MyMessage>> Fetch(IAdminManager adm)
    {
        // New messages are at the beginning by default
        var lst = await adm.GetAllMessagesFromSequencedList(name);
        if (lst.Count == 0)
        {
            Result = Results.Json(new {});
        }

        if (count < 0)
        {
            this.count = lst.Count;
        }

        var filtered = new List<MyMessage>();
        for (long i=start; i < start + count; i++)
        {
            var mm = lst.FirstOrDefault(item => item.Sequence == i);
            if (mm != null)
            {
                filtered.Add(mm);
            }
        }

        if (filtered.Count == 0)
        {
            Result = Results.Json(new {});
        }

        return filtered;
    }
}