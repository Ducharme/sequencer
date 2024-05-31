using DatabaseAccessLayer;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class DatabaseController : ControllerBase
{
    private readonly IDatabaseAdmin _databaseAdmin;

    public DatabaseController(IDatabaseAdmin databaseAdmin)
    {
        _databaseAdmin = databaseAdmin;
    }

    [HttpGet]
    public IActionResult GetAllMessages(string name)
    {
        var messages = _databaseAdmin.GetAllMessages(name);
        return Ok(messages);
    }
}
