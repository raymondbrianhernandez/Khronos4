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
            // Check if Email and Password are provided
            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
            {
                string err = "Email and Password are required.";
                return new JsonResult(new { success = false, error = err });
            }

            // Look up the user by Email
            var user = _context.Users.FirstOrDefault(u => u.Email == Email);
            if (user == null)
            {
                string err = "User not found.";
                return new JsonResult(new { success = false, error = err });
            }

            // Compute the hashed password
            var hashedPassword = HashPassword(Password);
            if (user.PasswordHash != hashedPassword)
            {
                string err = "Invalid password.";
                return new JsonResult(new { success = false, error = err });
            }

            // Check that the user is a Super Admin
            if (user.AdminRole != "Super Admin")
            {
                string err = "Only Super Administrators are allowed, please use the regular login page.";
                return new JsonResult(new { success = false, error = err });
            }

            // Create authentication claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim("FullName", $"{user.FirstName} {user.LastName}"),
                new Claim("AdminRole", user.AdminRole),
                new Claim("Congregation", user.Congregation ?? "Unassigned")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true
            };

            // Sign in the user
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // Return JSON success response for AJAX requests
            return new JsonResult(new { success = true });
        }

        // Helper method for password hashing using SHA256
        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
