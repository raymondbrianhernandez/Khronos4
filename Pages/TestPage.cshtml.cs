using Khronos4.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore; // Add this line

namespace Khronos4.Pages
{
    public class TestPageModel : PageModel // Remove BasePageModel
    {
        private readonly AppDbContext _context;

        public TestPageModel(AppDbContext context)
        {
            _context = context;
        }

        public async Task OnGetAsync()
        {
            await TestConnection();
            await LoadEldersAndServantsDropdown();
        }

        private async Task TestConnection()
        {
            try
            {
                using (var connection = _context.Database.GetDbConnection())
                {
                    await connection.OpenAsync();
                    ViewData["TestConnection"] = "Connection opened successfully.";
                    await connection.CloseAsync();
                    ViewData["TestConnection"] += "Connection closed successfully.";
                }
            }
            catch (Exception ex)
            {
                ViewData["TestConnection"] = $"Error testing connection: {ex.Message}";
            }
        }

        private async Task LoadEldersAndServantsDropdown()
        {
            try
            {
                var connectionString = _context.Database.GetConnectionString();
                if (string.IsNullOrEmpty(connectionString))
                {
                    ViewData["LoadEldersAndServantsDropdown"] = "ConnectionString is EMPTY.";
                }
                else
                {
                    ViewData["LoadEldersAndServantsDropdown"] = $"ConnectionString: {connectionString}";
                }
            }
            catch (System.Exception ex)
            {
                ViewData["LoadEldersAndServantsDropdown"] = $"Error in LoadEldersAndServantsDropdown: {ex.Message}";
            }
        }
    }
}