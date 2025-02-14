using System;
using System.ComponentModel.DataAnnotations;

namespace Khronos4.Models
{
    public class BlogRevision
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string RevisionDate { get; set; }

        [Required]
        [MaxLength(500)]
        public string Note { get; set; }

        [MaxLength(100)]
        public string Revision { get; set; }

        [MaxLength(100)]
        public string Category { get; set; }

        [Required]
        [MaxLength(100)]
        public string Author { get; set; }
    }
}
