using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Khronos4.Models
{
    public class OCLMScheduleViewModel
    {
        public string CongregationName { get; set; }
        public string WeeklyBibleVerses { get; set; }
        public TimeSpan StartTime { get; set; }

        public List<SelectListItem> EldersAndServantsDropdown { get; set; }
        public List<SelectListItem> PublishersDropdown { get; set; }
        public List<SelectListItem> BrothersDropdown { get; set; }
        public string SelectedMeetingWeekText { get; set; }
        public List<OCLMPart>? ExistingParts { get; set; }
        public string ExistingWeekText { get; set; }
        public bool IsFromDatabase { get; set; }
        public DateTime? SavedDate { get; set; }

        [BindProperty]
        public List<int> PartIDs { get; set; } = new();

        public Dictionary<string, string> ScrapedData { get; set; } = new();
    }

}
