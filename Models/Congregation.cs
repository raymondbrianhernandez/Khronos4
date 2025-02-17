using System.ComponentModel.DataAnnotations;

namespace Khronos4.Models
{
    public class Congregation
    {
        [Key]
        public int CongID { get; set; }
        
        public string Name { get; set; } = string.Empty; // Ensure a default value
        
        public string? Language { get; set; } // Nullable
        
        public string? Address { get; set; } // Nullable
        
        public string? City { get; set; } // Nullable
        
        public string? State { get; set; } // Nullable
        
        public string? Country { get; set; } // Nullable
        
        public string? CongCOBE { get; set; } // Nullable
        
        public string? CongSect { get; set; } // Nullable
        
        public string? CongSO { get; set; } // Nullable
    }

}
