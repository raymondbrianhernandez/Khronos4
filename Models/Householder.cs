namespace Khronos4.Models
{
    public class Householder
    {
        public int Id { get; set; } // Assuming there's an ID column
        public string Name { get; set; }
        public string Address { get; set; }
        public string Suite { get; set; }
        public string City { get; set; }
        public string Province { get; set; }
        public string Postal_Code { get; set; }
        public string Country { get; set; }
        public string Telephone { get; set; }
        public string Notes { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string Status { get; set; }
        public string Language { get; set; }
        public DateTime CreatedAt { get; set; } // Optional
    }
}
