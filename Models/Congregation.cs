using System.ComponentModel.DataAnnotations;

namespace Khronos4.Models
{
    public class Congregation
    {
        [Key]
        public int CongID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Language { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }  // Added ZipCode
        public string? Country { get; set; }
        public string? CongCOBE { get; set; }
        public string? CongSect { get; set; }
        public string? CongSO { get; set; }
        public string? MidweekDate { get; set; }  // Changed from DateTime? to string?
        public TimeSpan? MidweekTime { get; set; }
        public string? WeekendDate { get; set; }  // Changed from DateTime? to string?
        public TimeSpan? WeekendTime { get; set; }
        public string? Circuit { get; set; }
    }
}
