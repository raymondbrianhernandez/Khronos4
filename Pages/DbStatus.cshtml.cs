using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace Khronos4.Pages
{
    public class DbStatusModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public DbStatusModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                }
                // Return status "Online" if connection is successful.
                return new JsonResult(new { status = "Online" });
            }
            catch
            {
                // Return status "Offline" if an exception occurs.
                return new JsonResult(new { status = "Offline" });
            }
        }
    }
}
