using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Khronos4.Models
{
    [Table("ServiceGroups")]
    public class ServiceGroup
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CongregationID { get; set; } // Foreign Key to Congregations Table

        [Required]
        public int ServiceGroupNumber { get; set; } // The Service Group Number

        public string? Overseer { get; set; } // Overseer ID (Stored as string for now, can be changed to int if linking directly)

        public string? Assistant { get; set; } // Assistant ID (Stored as string for now, can be changed to int if linking directly)

        // Navigation Property (If linking to Congregations Table)
        [ForeignKey("CongregationID")]
        public Congregation? Congregation { get; set; }
    }
}
