namespace Khronos4.Models
{
    public class CSVPublisher
    {
        public string Name { get; set; }  // Required
        public int ServiceGroup { get; set; }  // Required (INT)
        public string Privilege { get; set; }  // Required
        public string PhoneNumber { get; set; } // Optional
    }
}
