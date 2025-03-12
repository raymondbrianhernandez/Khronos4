namespace Khronos4.Models
{
    public class Publisher
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Congregation { get; set; }  // ✅ Keep Congregation as int
        public string CongregationName { get; set; }  // ✅ Add this property to store the actual name
        public int ServiceGroup { get; set; }
        public string Privilege { get; set; }
        public bool IsRP { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Status { get; set; }
        public string Notes { get; set; }
    }
}
