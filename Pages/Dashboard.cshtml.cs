using Khronos4.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.SqlClient;
using System.Data;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.RegularExpressions;
using Khronos4.Models;
using System.Text.Json;

namespace Khronos4.Pages
{
    [Authorize]
    public class DashboardModel : PageModel
    {
        private readonly AppDbContext _context;
        public string UserEmail { get; set; }
        public string UserFullName { get; set; }
        public string CongregationName { get; set; }
        public string JWMeetingUrl { get; private set; }

        public DashboardModel(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
            var user = HttpContext.User;

            // Redirect to login if not authenticated
            if (user.Identity == null || !user.Identity.IsAuthenticated)
            {
                return RedirectToPage("Login");
            }

            // Retrieve User Info from Claims
            UserEmail = user.Identity.Name;
            UserFullName = user.Claims.FirstOrDefault(c => c.Type == "FullName")?.Value ?? "Unknown User";
            string congregationId = user.Claims.FirstOrDefault(c => c.Type == "Congregation")?.Value ?? "0";

            // Fetch Congregation Name using Stored Procedure
            CongregationName = GetCongregationName(congregationId);

            // Fetch the current JW Workbook Week
            JWMeetingUrl = GenerateJWMeetingUrl(DateTime.Now);

            return Page();
        }

        [HttpPost]
        public async Task<IActionResult> OnPostUpdatePlacementAsync([FromBody] Placement placementData)
        {
            if (placementData == null || string.IsNullOrWhiteSpace(placementData.Owner))
            {
                return new JsonResult(new { success = false, message = "Missing required fields." }) { StatusCode = 400 };
            }

            var parameters = new[]
            {
            new SqlParameter("@Owner", System.Data.SqlDbType.NVarChar) { Value = placementData.Owner },
            new SqlParameter("@Date", System.Data.SqlDbType.Date) { Value = placementData.Date },
            new SqlParameter("@Hours", System.Data.SqlDbType.Decimal) { Value = placementData.Hours },
            new SqlParameter("@Placements", System.Data.SqlDbType.Int) { Value = placementData.Placements },
            new SqlParameter("@RVs", System.Data.SqlDbType.Int) { Value = placementData.RVs },
            new SqlParameter("@BS", System.Data.SqlDbType.Int) { Value = placementData.BS },
            new SqlParameter("@LDC", System.Data.SqlDbType.Decimal) { Value = placementData.LDC },
            new SqlParameter("@Notes", System.Data.SqlDbType.NVarChar) { Value = string.IsNullOrWhiteSpace(placementData.Notes) ? (object)DBNull.Value : placementData.Notes }
        };

            int rowsAffected = await _context.Database.ExecuteSqlRawAsync(
                "EXEC [dbo].[UpdatePlacement] @Owner, @Date, @Hours, @Placements, @RVs, @BS, @LDC, @Notes",
                parameters);

            var result = new { success = true, rowsUpdated = rowsAffected };
            return new JsonResult(result);
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

        private string GenerateJWMeetingUrl(DateTime currentDate)
        {
            // Determine the workbook period (Jan-Feb, Mar-Apr, etc.)
            string[] months = { "january-february", "march-april", "may-june", "july-august", "september-october", "november-december" };
            int periodIndex = (currentDate.Month - 1) / 2; // Determine the period index
            string monthRange = months[periodIndex];

            // Get the start and end of the current week
            DateTime startOfWeek = currentDate.AddDays(-(int)currentDate.DayOfWeek + 1); // Monday
            DateTime endOfWeek = startOfWeek.AddDays(6); // Sunday

            // Format the schedule without commas
            string meetingSchedule = $"{startOfWeek.ToString("MMMM d", CultureInfo.InvariantCulture)}-{endOfWeek.Day} {currentDate.Year}";

            // Replace spaces with dashes and remove commas
            string formattedSchedule = Regex.Replace(meetingSchedule, @"\s+", "-");

            // Construct the final JW.org URL
            return $"https://www.jw.org/en/library/jw-meeting-workbook/{monthRange}-{currentDate.Year}-mwb/Life-and-Ministry-Meeting-Schedule-for-{formattedSchedule}/";
        }
    }
}
