using Khronos4.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Khronos4.Pages
{
    public class RegisterModel : PageModel
    {
        private readonly AppDbContext _context;

        public RegisterModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty] public string FirstName { get; set; }
        [BindProperty] public string LastName { get; set; }
        [BindProperty] public string Suffix { get; set; }
        [BindProperty] public string Congregation { get; set; }
        [BindProperty] public string Language { get; set; }
        [BindProperty] public string Email { get; set; }
        [BindProperty] public string Password { get; set; }
        [BindProperty] public string ConfirmPassword { get; set; }
        [BindProperty] public string Phone { get; set; }
        [BindProperty] public string SecurityQuestion1 { get; set; }
        [BindProperty] public string SecurityAnswer1 { get; set; }
        [BindProperty] public string SecurityQuestion2 { get; set; }
        [BindProperty] public string SecurityAnswer2 { get; set; }
        [BindProperty] public string AdminRole { get; set; }
        [BindProperty] public string Goal { get; set; }
        [BindProperty] public string ElderName { get; set; }
        [BindProperty] public string ElderEmail { get; set; }
        [BindProperty] public string ElderPhone { get; set; }

        public string ErrorMessage { get; set; }

		public List<SelectListItem> Congregations { get; set; }

		public async Task OnGetAsync()
		{
			Congregations = new List<SelectListItem>();

			using (var connection = _context.Database.GetDbConnection())
			{
				await connection.OpenAsync();
				using (var command = connection.CreateCommand())
				{
					command.CommandText = "dbo.GetCongregations";
					command.CommandType = System.Data.CommandType.StoredProcedure;

					using (var reader = await command.ExecuteReaderAsync())
					{
						while (await reader.ReadAsync())
						{
							Congregations.Add(new SelectListItem
							{
								Value = reader["CongID"].ToString(),
								Text = reader["Name"].ToString()
							});
						}
					}
				}
			}
		}

		public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password) || string.IsNullOrEmpty(ConfirmPassword))
            {
                ErrorMessage = "Email and Password are required.";
                return Page();
            }

            // 🔹 Validate Password Match
            if (Password != ConfirmPassword)
            {
                ErrorMessage = "Passwords do not match.";
                return Page();
            }

            try
            {
                using (var connection = _context.Database.GetDbConnection())
                {
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "dbo.RegisterUser";
                        command.CommandType = System.Data.CommandType.StoredProcedure;

                        command.Parameters.Add(new SqlParameter("@FirstName", FirstName));
                        command.Parameters.Add(new SqlParameter("@LastName", LastName));
                        command.Parameters.Add(new SqlParameter("@Email", Email));
                        command.Parameters.Add(new SqlParameter("@PasswordHash", HashPassword(Password)));
                        command.Parameters.Add(new SqlParameter("@SecurityQuestion1", SecurityQuestion1));
                        command.Parameters.Add(new SqlParameter("@SecurityAnswer1", SecurityAnswer1));
                        command.Parameters.Add(new SqlParameter("@SecurityQuestion2", SecurityQuestion2));
                        command.Parameters.Add(new SqlParameter("@SecurityAnswer2", SecurityAnswer2));
                        command.Parameters.Add(new SqlParameter("@Congregation", int.TryParse(Congregation, out int congId) ? congId : (object)DBNull.Value));

                        // Handle Optional Fields
                        command.Parameters.Add(new SqlParameter("@Suffix", string.IsNullOrEmpty(Suffix) ? (object)DBNull.Value : Suffix));
                        command.Parameters.Add(new SqlParameter("@Phone", string.IsNullOrEmpty(Phone) ? (object)DBNull.Value : Phone));
                        command.Parameters.Add(new SqlParameter("@Language", string.IsNullOrEmpty(Language) ? (object)DBNull.Value : Language));
                        command.Parameters.Add(new SqlParameter("@AdminRole", string.IsNullOrEmpty(AdminRole) ? (object)DBNull.Value : AdminRole));
                        command.Parameters.Add(new SqlParameter("@Goal", string.IsNullOrEmpty(Goal) ? (object)DBNull.Value : Goal));
                        command.Parameters.Add(new SqlParameter("@ElderName", string.IsNullOrEmpty(ElderName) ? (object)DBNull.Value : ElderName));
                        command.Parameters.Add(new SqlParameter("@ElderEmail", string.IsNullOrEmpty(ElderEmail) ? (object)DBNull.Value : ElderEmail));
                        command.Parameters.Add(new SqlParameter("@ElderPhone", string.IsNullOrEmpty(ElderPhone) ? (object)DBNull.Value : ElderPhone));

                        // Handle OUTPUT Parameter for AccountCreated
                        var accountCreatedParam = new SqlParameter("@AccountCreated", System.Data.SqlDbType.DateTime);
                        accountCreatedParam.Direction = System.Data.ParameterDirection.Output;
                        command.Parameters.Add(accountCreatedParam);

                        await command.ExecuteNonQueryAsync();

                        TempData["SuccessMessage"] = "Thanks for creating an account! You can now log in.";

                        return RedirectToPage("Login");
                    }
                }
            }
            catch (SqlException ex)
            {
                ErrorMessage = $"SQL Error {ex.Number}: {ex.Message}";
                return Page();
            }

        }

        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(password);
                byte[] hash = sha256.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }

    }
}
