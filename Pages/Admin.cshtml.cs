using Khronos4.Data;
using Khronos4.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace Khronos4.Pages
{
    [IgnoreAntiforgeryToken]
    public class AdminModel : PageModel
    {
        private readonly AppDbContext _context;

        public AdminModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string Email { get; set; }

        [BindProperty]
        public string Password { get; set; }

        public string ErrorMessage { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            // Validate input
            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
            {
                return new JsonResult(new { success = false, error = "Email and Password are required." });
            }

            // Lookup user in database
            var user = _context.Users.FirstOrDefault(u => u.Email == Email);
            if (user == null)
            {
                return new JsonResult(new { success = false, error = "User not found." });
            }

            // Hash the entered password and compare with stored hash
            var hashedPassword = HashPassword(Password.Trim());
            if (user.PasswordHash != hashedPassword)
            {
                return new JsonResult(new { success = false, error = "Incorrect email/password." });
            }

            // Ensure only Super Admins can access
            if (user.AdminRole?.Trim().ToLower() != "super admin")
            {
                return new JsonResult(new { success = false, error = "Only Super Administrators are allowed, please use the regular login page." });
            }

            // Create authentication claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim("FullName", $"{user.FirstName} {user.LastName}"),
                new Claim("AdminRole", user.AdminRole.Trim()),  // Ensure no spaces
                new Claim("Congregation", user.Congregation?.Trim() ?? "Unassigned")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties { IsPersistent = true };

            // Sign in user
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

            return new JsonResult(new { success = true });
        }

        // Helper method for hashing password
        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
