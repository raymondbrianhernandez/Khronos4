/*
    Khronos 4 by Raymond Hernandez
    January 28, 2025
*/

using System.Security.Cryptography;
using System.Text;
using Khronos4.Models;
using Khronos4.Data;

public static class DatabaseInitializer
{
    public static void Seed(AppDbContext dbContext)
    {
        if (!dbContext.Users.Any())
        {
            var hashedPassword = Convert.ToBase64String(
                SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes("password123"))
            );

            dbContext.Users.Add(new User
            {
                FirstName = "Raymond",
                LastName = "Hernandez",
                Email = "test@khronos.pro",
                PasswordHash = hashedPassword
            });
            dbContext.SaveChanges();
        }
    }
}

