/*
    Khronos 4 by Raymond Hernandez
    January 27, 2025
*/

using System;
using System.ComponentModel.DataAnnotations;

namespace Khronos4.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string? FirstName { get; set; }  // Nullable to prevent NullReferenceException

        [Required]
        [MaxLength(100)]
        public string? LastName { get; set; }

        [MaxLength(20)]
        public string? Suffix { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(200)]
        public string Email { get; set; } = string.Empty; // Prevents null issue

        [Required]
        public string? PasswordHash { get; set; }

        [MaxLength(15)]
        public string? Phone { get; set; }

        [MaxLength(200)]
        public string? Congregation { get; set; }

        [MaxLength(50)]
        public string? Language { get; set; }

        [MaxLength(50)]
        public string? AdminRole { get; set; }

        [MaxLength(500)]
        public string? Goal { get; set; }

        [MaxLength(200)]
        public string? SecurityQuestion1 { get; set; }

        [MaxLength(200)]
        public string? SecurityAnswer1 { get; set; }

        [MaxLength(200)]
        public string? SecurityQuestion2 { get; set; }

        [MaxLength(200)]
        public string? SecurityAnswer2 { get; set; }

        [MaxLength(200)]
        public string? ElderName { get; set; }

        [MaxLength(200)]
        public string? ElderEmail { get; set; }

        [MaxLength(15)]
        public string? ElderPhone { get; set; }

        public DateTime AccountCreated { get; set; } = DateTime.Now;

        public DateTime? LastLogin { get; set; }
    }
}

