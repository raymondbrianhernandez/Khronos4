using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Globalization;

namespace Khronos4.Pages
{
    public class PlacementsModel : PageModel
    {
        public string CurrentMonth { get; set; }
        public int CurrentYear { get; set; }

        // Bind from query string (or POST) so you can accept ?date=2025-02-01
        [BindProperty(SupportsGet = true)]
        public DateTime? Date { get; set; }

        public void OnGet()
        {
            // Use the provided date if available, else default to today.
            DateTime date = Date ?? DateTime.Now;
            CurrentMonth = date.ToString("MMMM", CultureInfo.InvariantCulture);
            CurrentYear = date.Year;
        }

        public IActionResult OnPost()
        {
            // Similar logic for POST, if needed.
            DateTime date = Date ?? DateTime.Now;
            CurrentMonth = date.ToString("MMMM", CultureInfo.InvariantCulture);
            CurrentYear = date.Year;
            return Page();
        }
    }
}
