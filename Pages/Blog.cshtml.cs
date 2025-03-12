using Khronos4.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Khronos4.Data;
using System.Data;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;

namespace Khronos4.Pages
{
    public class BlogModel : PageModel
    {
        private readonly AppDbContext _context;

        public BlogModel(AppDbContext context)
        {
            _context = context;
        }

        public List<BlogRevision> Revisions { get; set; } = new();

        public void OnGet()
        {
            var revisions = new List<BlogRevision>();

            using (var connection = _context.Database.GetDbConnection())
            {
                if (connection.State != ConnectionState.Open)
                    connection.Open();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "dbo.GetBlog"; // Stored Procedure Name
                    command.CommandType = CommandType.StoredProcedure;

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            revisions.Add(new BlogRevision
                            {
                                Id = reader.IsDBNull(0) ? 0 : reader.GetInt32(0),
                                RevisionDate = reader.IsDBNull(1) ? "" : reader.GetString(1),
                                Note = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                Revision = reader.IsDBNull(3) ? "" : reader.GetString(3),
                                Category = reader.IsDBNull(4) ? "" : reader.GetString(4),
                                Author = reader.IsDBNull(5) ? "Unknown" : reader.GetString(5)
                            });
                        }
                    }
                }
            }

            Revisions = revisions; // Assign to the property in your Razor Page
        }
    }
}
