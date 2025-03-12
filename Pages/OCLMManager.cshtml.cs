using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Khronos4.Helpers;
using Khronos4.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Khronos4.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Khronos4.Pages
{
    [Authorize]
    public class OCLMManagerModel : PageModel
    {
        private readonly JWMeetingScraper _scraper = new();
        private readonly AppDbContext _context;

        [BindProperty]
        public int SelectedYear { get; set; } = DateTime.Now.Year;
        [BindProperty]
        public string OpeningSong { get; set; } = "Click the button to load the song";
        public List<int> AvailableYears { get; set; }
        public List<string> WeeklyMeetingLinks { get; set; }
        public string CongregationName { get; set; }

        public OCLMManagerModel(AppDbContext context)
        {
            _context = context;
        }

        public void OnGet(int? year)
        {
            var user = HttpContext.User;

            AvailableYears = GenerateYearList();
            SelectedYear = year ?? DateTime.Now.Year;
            WeeklyMeetingLinks = GenerateWeeklyMeetingLinks(SelectedYear);

            // Retrieve User Info from Claims
            string congregationId = user.Claims.FirstOrDefault(c => c.Type == "Congregation")?.Value ?? "0";
            var userRole = User.Claims.FirstOrDefault(c => c.Type == "AdminRole")?.Value?.Trim().ToLower();
            CongregationName = GetCongregationName(congregationId);
        }

        private string GetCongregationName(string congregationId)
        {
            string congregationName = "Unassigned";
            using (var connection = _context.Database.GetDbConnection())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "dbo.GetCongregationName"; // Stored Procedure Name
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@CongID", congregationId));

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            congregationName = reader.GetString(0);
                        }
                    }
                }
            }
            return congregationName;
        }

        private List<int> GenerateYearList()
        {
            int currentYear = DateTime.Now.Year;
            return new List<int> { currentYear - 1, currentYear, currentYear + 1 };
        }

        public async Task<IActionResult> OnGetScrapeMeetingDetailsAsync(string meetingUrl)
        {
            if (string.IsNullOrEmpty(meetingUrl))
            {
                return new JsonResult(new { error = "No URL selected" });
            }

            try
            {
                var meetingDetails = await _scraper.ScrapeMeetingDetailsAsync(meetingUrl);
                return new JsonResult(meetingDetails);
            }
            catch (Exception ex)
            {
                return new JsonResult(new { error = $"Error scraping meeting details: {ex.Message}" });
            }
        }


        private List<string> GenerateWeeklyMeetingLinks(int year)
        {
            List<string> links = new List<string>();
            DateTime startDate = new DateTime(year, 1, 1);

            while (startDate.DayOfWeek != DayOfWeek.Monday)
            {
                startDate = startDate.AddDays(1);
            }

            while (startDate.Year == year || (startDate.Year == year + 1 && startDate.Month == 1))
            {
                DateTime endDate = startDate.AddDays(6);
                string startMonth = startDate.ToString("MMMM", CultureInfo.InvariantCulture);
                string endMonth = endDate.ToString("MMMM", CultureInfo.InvariantCulture);
                string monthRange = GetMonthRange(startDate.Month, startDate.Year);

                string formattedDate = startMonth == endMonth
                    ? $"{startMonth}-{startDate.Day}-{endDate.Day}-{startDate.Year}"
                    : $"{startMonth}-{startDate.Day}-{startDate.Year}-{endMonth}-{endDate.Day}-{endDate.Year}";

                string url = $"https://www.jw.org/en/library/jw-meeting-workbook/{monthRange}-mwb/Life-and-Ministry-Meeting-Schedule-for-{formattedDate}/";
                links.Add(FormatURL(url));

                startDate = startDate.AddDays(7);
            }

            return links;
        }

        private string GetMonthRange(int month, int year)
        {
            string[] monthRanges = { "january-february", "march-april", "may-june", "july-august", "september-october", "november-december" };
            int periodIndex = (month - 1) / 2;
            return $"{monthRanges[periodIndex]}-{year}";
        }

        private string FormatURL(string link)
        {
            return Regex.Replace(link, @"(\w+)-(\d+)-(\d{4})-(\w+)-(\d+)-\3/", "$1-$2-$4-$5-$3/");
        }

        public string GetDisplayText(string link)
        {
            var match = Regex.Match(link, @"Life-and-Ministry-Meeting-Schedule-for-([\w]+)-(\d+)(?:-(\d{4}))?-(?:([\w]+)-)?(\d+)-(\d{4})/");
            if (match.Success)
            {
                string startMonth = match.Groups[1].Value;
                string startDay = match.Groups[2].Value;
                string startYear = match.Groups[3].Value;
                string endMonth = match.Groups[4].Value;
                string endDay = match.Groups[5].Value;
                string endYear = match.Groups[6].Value;

                return string.IsNullOrEmpty(endMonth)
                    ? $"{startMonth} {startDay}-{endDay}"
                    : $"{startMonth} {startDay}-{endMonth} {endDay}";
            }

            return "Invalid Date";
        }
    }
}
