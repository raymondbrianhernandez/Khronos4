using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Khronos4.Data;
using Khronos4.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Khronos4.Pages
{
    public class AdminDashboardModel : PageModel
    {
        private readonly AppDbContext _context;

        public AdminDashboardModel(AppDbContext context)
        {
            _context = context;
        }

        // These properties will hold the data from the stored procedures / tables.
        public List<Congregation> Congregations { get; set; }
        public List<User> Users { get; set; }
        public List<BlogRevision> BlogRevisions { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Retrieve the user's admin role from their claims
            var userRole = User.Claims.FirstOrDefault(c => c.Type == "AdminRole")?.Value?.Trim().ToLower();

            // If user is already a "Super Admin", redirect them to AdminDashboard
            if (userRole != "super admin")
            {
                return RedirectToPage("/Dashboard");
            }

            // Load Congregations from stored procedure
            Congregations = await _context.Congregations
                .FromSqlRaw("EXEC dbo.GetCongregations")
                .ToListAsync();

            // Load Users (assuming you want to display all users)
            Users = await _context.Users.ToListAsync();

            // Load Blog entries from stored procedure
            BlogRevisions = await _context.BlogRevisions
                .FromSqlRaw("EXEC dbo.GetBlog")
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnPostAddCongregationAsync()
        {
            try
            {
                // Retrieve form data safely
                string name = Request.Form["name"];
                string language = Request.Form["language"];
                string address = Request.Form["address"];
                string city = Request.Form["city"];
                string state = Request.Form["state"];
                string country = Request.Form["country"];
                string congCOBE = Request.Form["congCOBE"];
                string congSect = Request.Form["congSect"];
                string congSO = Request.Form["congSO"];

                // Required fields validation
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(language))
                {
                    ModelState.AddModelError("", "Congregation Name and Language are required.");
                    return Page();
                }

                // Prepare SQL parameters ensuring NULL handling
                var parameters = new[]
                {
                    new SqlParameter("@Name", name),
                    new SqlParameter("@Language", language),
                    new SqlParameter("@Address", string.IsNullOrWhiteSpace(address) ? (object)DBNull.Value : address),
                    new SqlParameter("@City", string.IsNullOrWhiteSpace(city) ? (object)DBNull.Value : city),
                    new SqlParameter("@State", string.IsNullOrWhiteSpace(state) ? (object)DBNull.Value : state),
                    new SqlParameter("@Country", string.IsNullOrWhiteSpace(country) ? (object)DBNull.Value : country),
                    new SqlParameter("@CongCOBE", string.IsNullOrWhiteSpace(congCOBE) ? (object)DBNull.Value : congCOBE),
                    new SqlParameter("@CongSect", string.IsNullOrWhiteSpace(congSect) ? (object)DBNull.Value : congSect),
                    new SqlParameter("@CongSO", string.IsNullOrWhiteSpace(congSO) ? (object)DBNull.Value : congSO)
                };

                // Ensure _context is initialized before executing SQL
                if (_context == null)
                {
                    throw new NullReferenceException("_context is not initialized.");
                }

                await _context.Database.ExecuteSqlRawAsync("EXEC dbo.AddCongregation @Name, @Language, @Address, @City, @State, @Country, @CongCOBE, @CongSect, @CongSO", parameters);

                return RedirectToPage("/AdminDashboard");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"An error occurred: {ex.Message}");
                return Page();
            }
        }


        private bool IsSuperAdmin()
        {
            var userRole = User.Claims.FirstOrDefault(c => c.Type == "AdminRole")?.Value;
            return userRole == "Super Admin";
        }

        // DELETE handlers for each section

        public async Task<IActionResult> OnGetDeleteCongregationAsync(int id)
        {
            using (var command = _context.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = "EXEC dbo.DeleteCongregation @Id";
                command.Parameters.Add(new SqlParameter("@Id", id));
                await _context.Database.OpenConnectionAsync();
                await command.ExecuteNonQueryAsync();
                _context.Database.CloseConnection();
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnGetDeleteUserAsync(int id)
        {
            using (var command = _context.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = "EXEC dbo.DeleteUser @Id";
                command.Parameters.Add(new SqlParameter("@Id", id));
                await _context.Database.OpenConnectionAsync();
                await command.ExecuteNonQueryAsync();
                _context.Database.CloseConnection();
            }
            return RedirectToPage();
        }

        public async Task<IActionResult> OnGetDeleteBlogAsync(int id)
        {
            using (var command = _context.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = "EXEC dbo.DeleteBlog @Id";
                command.Parameters.Add(new SqlParameter("@Id", id));
                await _context.Database.OpenConnectionAsync();
                await command.ExecuteNonQueryAsync();
                _context.Database.CloseConnection();
            }
            return RedirectToPage();
        }
    }
}
