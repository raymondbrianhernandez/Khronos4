using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Linq;

namespace Khronos4.Pages
{
    [Authorize] // Only authenticated users can access this page
    public class AdminDashboardModel : PageModel
    {
        public IActionResult OnGet()
        {
            // Check if the logged-in user's AdminRole claim equals "Super Admin"
            var adminRole = User.Claims.FirstOrDefault(c => c.Type == "AdminRole")?.Value;
            if (adminRole != "Super Admin")
            {
                // If the user isn't a Super Admin, redirect them to the regular login page
                return RedirectToPage("/Admin");
            }

            return Page();
        }
    }
}
