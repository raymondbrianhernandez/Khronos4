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

        public async Task OnGetAsync()
        {
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
