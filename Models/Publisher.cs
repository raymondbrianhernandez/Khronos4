namespace Khronos4.Models
{
    public class Publisher
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Congregation { get; set; }  
        public string CongregationName { get; set; }  
        public int ServiceGroup { get; set; }
        public string Privilege { get; set; }
        public bool IsRP { get; set; }
        public bool IsCBSOverseer { get; set; } // New and its optional
        public bool IsCBSAssistant { get; set; } // New and its optional
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Status { get; set; }
        public string Notes { get; set; }
    }
}
