using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System;
using System.Globalization;
using System.Threading.Tasks;
using Khronos4.Data;
using Microsoft.EntityFrameworkCore;

namespace Khronos4.Pages
{
    public class PlacementsModel : PageModel
    {
        private readonly AppDbContext _context;

        public PlacementsModel(AppDbContext context)
        {
            _context = context;
        }

        public string CurrentMonth { get; set; }
        public int CurrentYear { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? Date { get; set; }

        [BindProperty]
        public int Hours { get; set; }
        [BindProperty]
        public int Placements { get; set; }
        [BindProperty]
        public int RVs { get; set; }
        [BindProperty]
        public int BS { get; set; }

        public void OnGet()
        {
            DateTime date = Date ?? DateTime.Now;
            CurrentMonth = date.ToString("MMMM", CultureInfo.InvariantCulture);
            CurrentYear = date.Year;
        }

        public async Task<IActionResult> OnPostUpdatePlacementAsync()
        {
            if (Date == null)
            {
                ModelState.AddModelError("", "Date is required.");
                return Page();
            }

            var owner = User.Identity.Name; // Adjust this if needed

            using (var command = _context.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = "EXEC dbo.UpdatePlacement @Owner, @Date, @Hours, @Placements, @RVs, @BS";
                command.Parameters.Add(new SqlParameter("@Owner", owner));
                command.Parameters.Add(new SqlParameter("@Date", Date));
                command.Parameters.Add(new SqlParameter("@Hours", Hours));
                command.Parameters.Add(new SqlParameter("@Placements", Placements));
                command.Parameters.Add(new SqlParameter("@RVs", RVs));
                command.Parameters.Add(new SqlParameter("@BS", BS));

                await _context.Database.OpenConnectionAsync();
                await command.ExecuteNonQueryAsync();
                _context.Database.CloseConnection();
            }

            return RedirectToPage();
        }
    }
}
