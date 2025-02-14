using Khronos4.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Khronos4.Data;
using System.Data;

namespace Khronos4.Pages
{
    public class BlogModel : PageModel
    {
        private readonly AppDbContext _context;

        public BlogModel(AppDbContext context)
        {
            _context = context;
        }

        public List<BlogRevision> Revisions { get; set; }

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
					command.CommandType = System.Data.CommandType.StoredProcedure;

					using (var reader = command.ExecuteReader())
					{
						while (reader.Read())
						{
							revisions.Add(new BlogRevision
							{
								Id = reader.GetInt32(0),
								RevisionDate = reader.GetString(1),
								Note = reader.GetString(2),
								Revision = reader.GetString(3),
								Category = reader.GetString(4),
								Author = reader.GetString(5)
							});
						}
					}
				}
			}

			Revisions = revisions; // Assign to the property in your Razor Page
		}
	}
}
