using AdminService;
using CommonTypes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebAdminPortal.Pages
{
    public class DatabaseMessagesModel : PageModel
    {
        [FromQuery(Name = "name")]
        public string? Name { get; private set; }
        public List<MyMessage>? Messages { get; private set; }

        private readonly IAdminManager adminManager;

        public DatabaseMessagesModel([FromServices] IAdminManager am)
        {
            this.adminManager = am;
        }

        public async void OnGetAsync(string name)
        {
            Messages = await adminManager.GetAllMessagesFromDatabase(name);
        }
    }
}
