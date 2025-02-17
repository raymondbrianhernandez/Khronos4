using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Khronos4.Data;
using Khronos4.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Khronos4.Pages
{
    public class AdminDashboardModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

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

        private async Task<List<Congregation>> LoadCongregationsFromDatabase()
        {
            var congregations = new List<Congregation>();
            // Retrieve the connection string from configuration
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            // Use a SqlConnection to open the connection
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "dbo.GetCongregations";  // Your stored procedure name
                    command.CommandType = System.Data.CommandType.StoredProcedure;

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            congregations.Add(new Congregation
                            {
                                CongID = reader.GetInt32(0),
                                Name = reader.IsDBNull(1) ? null : reader.GetString(1),
                                Language = reader.IsDBNull(2) ? null : reader.GetString(2),
                                Address = reader.IsDBNull(3) ? null : reader.GetString(3),
                                City = reader.IsDBNull(4) ? null : reader.GetString(4),
                                State = reader.IsDBNull(5) ? null : reader.GetString(5),
                                Country = reader.IsDBNull(6) ? null : reader.GetString(6),
                                CongCOBE = reader.IsDBNull(7) ? null : reader.GetString(7),
                                CongSect = reader.IsDBNull(8) ? null : reader.GetString(8),
                                CongSO = reader.IsDBNull(9) ? null : reader.GetString(9)
                            });
                        }
                    }
                }
            }

            return congregations;
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
