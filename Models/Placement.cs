namespace Khronos4.Models
{
    public class Placement
    {
        public int Id { get; set; }
        public string Owner { get; set; }
        public DateTime Date { get; set; }
        public decimal? Hours { get; set; }
        public int? Placements { get; set; }
        public int? RVs { get; set; }
        public int? BS { get; set; }
        public decimal? LDC { get; set; }
        public string Notes { get; set; } = "";
    }
}
