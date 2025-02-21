/*
    Khronos 4 by Raymond Hernandez
    January 28, 2025

    REVISIONS for Login.cshtml.cs
    2/4/2025 - Add CSS, and an option to "Remember Me"
*/

using Khronos4.Data;
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
using Khronos4.Models;

namespace Khronos4.Pages
{
    public class LoginModel : PageModel
    {
        private readonly AppDbContext _context;

        public LoginModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty] public string Email { get; set; }
        [BindProperty] public string Password { get; set; }
        [BindProperty] public bool RememberMe { get; set; }

        public string ErrorMessage { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
            {
                ErrorMessage = "Email and Password are required.";
                return Page();
            }

            var user = _context.Users.FirstOrDefault(u => u.Email == Email);
            if (user == null)
            {
                ErrorMessage = "User not found.";
                return Page();
            }

            var hashedPassword = HashPassword(Password);
            
            if (user.PasswordHash != hashedPassword)
            {
                ErrorMessage = "Invalid password.";
                return Page();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim("FullName", $"{user.FirstName} {user.LastName}"),
                new Claim("Congregation", user.Congregation ?? "Unassigned"),
                new Claim("AdminRole", user.AdminRole ?? "User")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = RememberMe
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

            return RedirectToPage("/Dashboard");
        }

        public List<BlogRevision> Revisions { get; set; }

        public void OnGet()
        {
            if (User.Identity.IsAuthenticated)
            {
                Response.Redirect("/Dashboard");
                return;
            }
            /*// Get the latest 5 revisions
            Revisions = _context.BlogRevisions
                .OrderByDescending(r => r.Id)
                .ToList();*/
        }

        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
