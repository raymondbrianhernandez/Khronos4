namespace Khronos4.Models
{
    public class OCLMPart
    {
        public int ID { get; set; }
        public int CongID { get; set; }
        public string WeekOf { get; set; }
        public TimeSpan StartTime { get; set; }
        public string Part { get; set; }
        public string? Assignee { get; set; }
        public string? Assistant { get; set; }
    }
}
