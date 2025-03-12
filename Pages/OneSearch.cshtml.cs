using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNetCore.Authorization;

namespace Khronos4.Pages
{
    [Authorize]
    public class ToolsModel : PageModel
    {
        [BindProperty] public string Street { get; set; }
        [BindProperty] public string City { get; set; }
        [BindProperty] public string State { get; set; }
        [BindProperty] public List<string> SelectedSites { get; set; }

        public IActionResult OnGet()
        {
            if (!User.Identity.IsAuthenticated) // 🔒 Additional check for non-authenticated users
            {
                return RedirectToPage("/Login");
            }
            return Page();
        }

        public IActionResult OnPost()
        {
            if (!User.Identity.IsAuthenticated)  // 🔒 Prevents unauthorized POST requests
            {
                return RedirectToPage("/Login");
            }

            if (string.IsNullOrWhiteSpace(Street) || string.IsNullOrWhiteSpace(City) || string.IsNullOrWhiteSpace(State))
            {
                ModelState.AddModelError("", "All fields are required.");
                return Page();
            }

            return RedirectToPage("/OneSearchResults", new { street = Street, city = City, state = State, sites = string.Join(",", SelectedSites) });
        }
    }
}
