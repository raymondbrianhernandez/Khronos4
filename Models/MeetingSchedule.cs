using System;
using System.ComponentModel.DataAnnotations;

namespace Khronos4.Models
{
    public class MeetingSchedule
    {
        [Key]
        public int Id { get; set; }
        public string CongName { get; set; }
        public DateTime Date { get; set; }
        public string OpeningSong { get; set; }
        public string Part1 { get; set; }
        public string Part1Time { get; set; }
        public string Part2 { get; set; }
        public string Part2Time { get; set; }
        public string Part3 { get; set; }
        public string Part3Time { get; set; }
        public string Part4 { get; set; }
        public string Part4Time { get; set; }
        public string Part5 { get; set; }
        public string Part5Time { get; set; }
        public string MidSong { get; set; }
        public string CBS { get; set; }
        public string CBSTime { get; set; }
        public string ClosingSong { get; set; }
        public DateTime ScrapedAt { get; set; } = DateTime.UtcNow;

        // ✅ Add ErrorMessage property
        public string ErrorMessage { get; set; }
    }
}
