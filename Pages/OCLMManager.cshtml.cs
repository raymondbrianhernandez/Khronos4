using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Text.RegularExpressions;
using Khronos4.Helpers;
using Khronos4.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Khronos4.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Json;
using System.Text;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Office.Word;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml.Office2010.Excel;

namespace Khronos4.Pages
{
    [Authorize]
    [IgnoreAntiforgeryToken]
    public class OCLMManagerModel : BasePageModel
    {
        private readonly JWMeetingScraper _scraper = new();
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        
        public OCLMManagerModel(IConfiguration configuration, AppDbContext context) : base(context)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        /* Scraping Info */
        [BindProperty] public string BibleVerse { get; set; } = "";
        [BindProperty] public int SelectedYear { get; set; } = DateTime.Now.Year;
        [BindProperty] public DateTime MeetingDate { get; set; }
        public List<int> AvailableYears { get; set; }
        public List<string> WeeklyMeetingLinks { get; set; }
        public Dictionary<string, string> MeetingData { get; set; } = new Dictionary<string, string>();
        public int MidweekTimeInMinutes { get; set; }
        public List<OCLMPart> ExistingParts { get; set; } = new List<OCLMPart>();
        public string ExistingWeekText { get; set; }

        /* Publishers Dropdown */
        public List<SelectListItem> EldersDropdown { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> EldersAndServantsDropdown { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> PublishersDropdown { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> BrothersDropdown { get; set; } = new List<SelectListItem>();
        [BindProperty] public List<string> EldersAndServantsList { get; set; } = new List<string>();
        [BindProperty] public List<string> EldersList { get; set; } = new List<string>();
        [BindProperty] public List<string> PublishersList { get; set; } = new List<string>();
        [BindProperty] public List<string> BrothersList { get; set; } = new List<string>();

        /* OCLM Parts to Export to DB */
        [BindProperty] public List<int> PartIDs { get; set; } = new();
        [BindProperty] public string CongregationName { get; set; }
        [BindProperty] public string SelectedMeetingWeekText { get; set; }
        [BindProperty] public DateTime Date { get; set; }
        [BindProperty] public List<string> StartTimes { get; set; }
        [BindProperty] public string? Chairman { get; set; }
        [BindProperty] public string OpeningSong { get; set; }
        [BindProperty] public string? OpeningPrayer { get; set; }
        [BindProperty] public string TreasuresTalkPart { get; set; }
        [BindProperty] public string? TreasuresTalkSpeaker { get; set; }
        [BindProperty] public string SpiritualGemsPart { get; set; }
        [BindProperty] public string? SpiritualGemsSpeaker { get; set; }
        [BindProperty] public string BibleReadingPart { get; set; }
        [BindProperty] public string? BibleReadingStudent { get; set; }
        [BindProperty] public List<string> StudentPartText { get; set; }
        [BindProperty] public List<string?> StudentAssignment { get; set; }
        [BindProperty] public List<string?> StudentAssistant { get; set; }
        [BindProperty] public string MiddleSong { get; set; }
        [BindProperty] public List<string> ElderAssignmentText { get; set; }
        [BindProperty] public List<string?> ElderAssignment { get; set; }
        [BindProperty] public string? CbsReader { get; set; }
        [BindProperty] public string ClosingSong { get; set; }
        [BindProperty] public string? ClosingPrayer { get; set; }
        [BindProperty] public List<string> AllAssignees { get; set; } = new();
        [BindProperty] public List<string> AllAssistants { get; set; } = new();
        [BindProperty] public List<string> Parts { get; set; }

        public async Task OnGetAsync(int? year, string meetingUrl = null)
        {
            if (_context == null)
            {
                TempData["Error"] += " ❌ Database context is NULL on page load.";
                return;
            }

            if (!await _context.Database.CanConnectAsync())
            {
                TempData["Error"] += " ❌ Database connection failed in OnGetAsync().";
                return;
            }

            var user = HttpContext.User;
            AvailableYears = GenerateYearList();
            SelectedYear = year ?? DateTime.Now.Year;
            WeeklyMeetingLinks = GenerateWeeklyMeetingLinks(SelectedYear);

            MeetingDate = DateTime.Now;
            BibleVerse = "";

            // Persist MeetingData across page reloads using ViewData
            if (TempData.ContainsKey("MeetingData"))
            {
                var tempDataValue = TempData["MeetingData"] as Dictionary<string, string>;
                MeetingData = tempDataValue ?? new Dictionary<string, string>();
            }

            string congregationId = user.Claims.FirstOrDefault(c => c.Type == "Congregation")?.Value ?? "0";
            CongregationName = GetCongregationName(congregationId);

            if (TempData.ContainsKey("MeetingData"))
            {
                var jsonString = TempData["MeetingData"] as string;
                if (!string.IsNullOrEmpty(jsonString))
                {
                    MeetingData = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonString);
                }
            }

            TempData["Error"] += $" ✅ Retrieved Congregation Name: {CongregationName}.";
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

        private List<int> GenerateYearList()
        {
            int currentYear = DateTime.Now.Year;
            return new List<int> { currentYear - 1, currentYear, currentYear + 1 };
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

        private async Task<TimeSpan> FetchMidweekTimeFromDb()
        {
            try
            {
                using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand("dbo.GetCongregationMidweekTime", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@CongID", CongregationID);

                        var result = await command.ExecuteScalarAsync();

                        if (result != null && TimeSpan.TryParse(result.ToString(), out TimeSpan midweekTime))
                        {
                            return midweekTime;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] += $"Error retrieving midweek time: {ex.Message}\n";
            }

            // Return default 7:00 PM if nothing found or error occurred
            return new TimeSpan(19, 0, 0);
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

        [HttpGet]
        public async Task<IActionResult> OnGetCongregationMidweekTime()
        {
            try
            {
                using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("dbo.GetCongregationMidweekTime", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@CongID", CongregationID);

                        var result = await command.ExecuteScalarAsync(); // Get the single value

                        if (result != null && TimeSpan.TryParse(result.ToString(), out TimeSpan midweekTime))
                        {
                            return new JsonResult(new { success = true, midweekTime = midweekTime.ToString(@"hh\:mm") });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = $"Error: {ex.Message}" });
            }

            return new JsonResult(new { success = false, message = "No data found." });
        }

        private async Task LoadEldersDropdown()
        {
            using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand("dbo.GetEldersFromCongregation", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@UserCongregation", CongregationID);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            EldersList.Add(reader.GetString(0)); // Add Elder's Name to the List
                        }
                    }
                }
            }
        }

        private async Task LoadEldersAndServantsDropdown()
        {
            try
            {
                if (_context == null)
                {
                    TempData["Error"] += " ❌ Database context is NULL in LoadEldersAndServantsDropdown().";
                    return;
                }

                var connection = _context.Database.GetDbConnection();
                if (connection.State == ConnectionState.Closed)
                {
                    await connection.OpenAsync();
                }

                TempData["Error"] += " ✅ Database connection is available. Attempting to load Elders and Servants...";

                EldersAndServantsDropdown = await _context.Publishers
                    .AsNoTracking()
                    .Where(p => p.Privilege == "Elder" || p.Privilege == "Servant")
                    .OrderBy(p => p.Name)
                    .Select(p => new SelectListItem
                    {
                        Value = p.Id.ToString(),
                        Text = p.Name
                    })
                    .ToListAsync();

                if (!EldersAndServantsDropdown.Any())
                {
                    TempData["Error"] += " 🚨 No Elders or Servants found in the database.";
                }
                else
                {
                    TempData["Error"] += $" ✅ Loaded {EldersAndServantsDropdown.Count} Elders/Servants.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] += $" ❌ Error in LoadEldersAndServantsDropdown: {ex.Message}";
            }
        }

        private async Task LoadPublishersDropdown()
        {
            try
            {
                if (_context == null)
                {
                    TempData["Error"] += " ❌ Database context is NULL in LoadPublishersDropdown().";
                    return;
                }

                using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand("dbo.GetPublishersByCongregation", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@CongregationId", CongregationID);

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            var tempList = new List<SelectListItem>();

                            while (await reader.ReadAsync())
                            {
                                tempList.Add(new SelectListItem
                                {
                                    Value = reader["Id"].ToString(),  // Unique ID
                                    Text = $"{reader["Name"]} ({reader["Privilege"]})",  // Display Name with Privilege
                                    Group = new SelectListGroup { Name = reader["Privilege"].ToString() } // Grouping
                                });
                            }

                            // ✅ Custom Sorting Order: Brother → Sister → Servant → Elder
                            PublishersDropdown = tempList
                                .OrderBy(p => p.Group.Name == "Brother" ? 1 :
                                              p.Group.Name == "Sister" ? 2 :
                                              p.Group.Name == "Servant" ? 3 : 4) // Elder is last
                                .ThenBy(p => p.Text) // Sort by Name within groups
                                .ToList();
                        }
                    }
                }

                if (!PublishersDropdown.Any())
                {
                    TempData["Error"] += " 🚨 No publishers found in the database.";
                }
                else
                {
                    TempData["Error"] += $" ✅ Loaded {PublishersDropdown.Count} publishers.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] += $" ❌ Error in LoadPublishersDropdown: {ex.Message}";
            }
        }

        private async Task LoadBrothersDropdown()
        {
            try
            {
                if (_context == null)
                {
                    TempData["Error"] += " ❌ Database context is NULL in LoadBrothersDropdown().";
                    return;
                }

                var connection = _context.Database.GetDbConnection();
                if (connection.State == ConnectionState.Closed)
                {
                    await connection.OpenAsync();
                }

                TempData["Error"] += " ✅ Attempting to load Brothers (Elder, Servant, Brother)...";

                var brotherRoles = new[] { "Elder", "Servant", "Brother" };

                var brothersDropdown = await _context.Publishers
                    .AsNoTracking()
                    .Where(p => brotherRoles.Contains(p.Privilege))
                    .OrderBy(p => p.Name)
                    .Select(p => new SelectListItem
                    {
                        Value = p.Id.ToString(),
                        Text = p.Name
                    })
                    .ToListAsync();

                if (!brothersDropdown.Any())
                {
                    TempData["Error"] += " 🚨 No brothers found in the database.";
                }
                else
                {
                    TempData["Error"] += $" ✅ Loaded {brothersDropdown.Count} brothers.";
                }

                BrothersDropdown = brothersDropdown;
            }
            catch (Exception ex)
            {
                TempData["Error"] += $" ❌ Error in LoadBrothersDropdown: {ex.Message}";
            }
        }

        private string GetCongregationName(string congregationId)
        {
            string congregationName = "Unassigned";
            using (var connection = _context.Database.GetDbConnection())
            {
                if (connection.State == ConnectionState.Closed) // Only open if it's closed
                {
                    connection.Open();
                }

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

        public async Task<IActionResult> OnGetScrapeMeetingDetailsAsync(string meetingUrl)
        {
            if (string.IsNullOrEmpty(meetingUrl))
                return Content("Please select a meeting.");

            try
            {
                var weekText = GetDisplayText(meetingUrl);

                // 🔍 First check if we already have data in DB
                var existingParts = await _context.OCLMParts
                    .Where(p => p.CongID == CongregationID && p.WeekOf == weekText)
                    .OrderBy(p => p.StartTime)
                    .ToListAsync();

                // Load dropdowns
                await LoadEldersAndServantsDropdown();
                await LoadPublishersDropdown();
                await LoadBrothersDropdown();

                CongregationName = GetCongregationName(CongregationID.ToString());
                var midweekTime = await FetchMidweekTimeFromDb();

                // 🔍 Try to load the saved Date (if any) for this week and congregation
                DateTime? savedDate = null;
                using (var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();

                    using (var cmd = new SqlCommand(@"SELECT Date FROM CongregationsMidweekDates WHERE CongID = @CongID AND WeekOf = @WeekOf", conn))
                    {
                        cmd.Parameters.AddWithValue("@CongID", CongregationID);
                        cmd.Parameters.AddWithValue("@WeekOf", weekText);

                        var result = await cmd.ExecuteScalarAsync();
                        if (result != null && result != DBNull.Value)
                            savedDate = (DateTime)result;
                    }
                }

                if (existingParts.Any())
                {
                    // ✅ Map DB values to the ViewModel
                    var viewModel = new OCLMScheduleViewModel
                    {
                        CongregationName = CongregationName,
                        SelectedMeetingWeekText = weekText,
                        StartTime = midweekTime,
                        SavedDate = savedDate != default ? savedDate : (DateTime?)null,
                        EldersAndServantsDropdown = EldersAndServantsDropdown,
                        PublishersDropdown = PublishersDropdown,
                        BrothersDropdown = BrothersDropdown,
                        ExistingParts = existingParts,
                        ExistingWeekText = weekText,
                        IsFromDatabase = true
                    };

                    TempData["Success"] = $"✅ Meeting data for {weekText} already exists. Loaded from database.";
                    return Partial("Shared/Partials/OCLM/_OCLMSchedulePartial", viewModel);
                }

                // 🔄 Otherwise, continue with scraping
                var data = await _scraper.ScrapeMeetingDetailsAsync(meetingUrl);
                if (data == null || data.Count == 0)
                {
                    TempData["Error"] = "❌ No data returned from the workbook URL.";
                    return Content("<p class='text-danger'>No data found.</p>", "text/html");
                }

                var scrapedViewModel = new OCLMScheduleViewModel
                {
                    CongregationName = CongregationName,
                    WeeklyBibleVerses = data.ContainsKey("Weekly Bible Verses") ? data["Weekly Bible Verses"] : "",
                    StartTime = midweekTime,
                    ScrapedData = data,
                    EldersAndServantsDropdown = EldersAndServantsDropdown,
                    PublishersDropdown = PublishersDropdown,
                    BrothersDropdown = BrothersDropdown,
                    SelectedMeetingWeekText = weekText,
                    ExistingParts = new List<OCLMPart>(), // Make sure this is empty
                    IsFromDatabase = false
                };

                TempData["Success"] = $"✅ Workbook loaded for {weekText}.";
                return Partial("Shared/Partials/OCLM/_OCLMSchedulePartial", scrapedViewModel);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"❌ Error: {ex.Message}";
                return Content("<p class='text-danger'>Error generating meeting schedule.</p>", "text/html");
            }
        }

        public async Task<IActionResult> OnGetCheckMeetingExistsAsync(string weekOf)
        {
            try
            {
                using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                await connection.OpenAsync();

                using var command = new SqlCommand("SELECT COUNT(*) FROM OCLMParts WHERE CongID = @CongID AND WeekOf = @WeekOf", connection);
                command.Parameters.AddWithValue("@CongID", CongregationID);
                command.Parameters.AddWithValue("@WeekOf", weekOf);

                int count = (int)await command.ExecuteScalarAsync();

                return new JsonResult(new { exists = count > 0 });
            }
            catch (Exception ex)
            {
                return new JsonResult(new { error = true, message = ex.Message });
            }
        }

        public async Task<IActionResult> OnPostUpdateMeeting()
        {
            var debugLog = new StringBuilder();
            debugLog.AppendLine("<b>🔍 Payload Sent to Database:</b><br/>");

            if (string.IsNullOrWhiteSpace(SelectedMeetingWeekText))
            {
                TempData["Error"] += "⚠️ Week selection cannot be empty.<br/>";
                return RedirectToPage();
            }

            if (CongregationID == 0)
            {
                TempData["Error"] += "⚠️ CongregationID is missing.<br/>";
                return RedirectToPage();
            }

            try
            {
                var partIds = Request.Form.Keys.Where(k => k.StartsWith("PartIDs[")).OrderBy(k => k).Select(k => Request.Form[k]).ToList();
                var parts = Request.Form.Keys.Where(k => k.StartsWith("AllParts[")).OrderBy(k => k).Select(k => Request.Form[k]).ToList();
                var assignees = Request.Form.Keys.Where(k => k.StartsWith("AllAssignees[")).OrderBy(k => k).Select(k => Request.Form[k]).ToList();
                var assistants = Request.Form.Keys.Where(k => k.StartsWith("AllAssistants[")).OrderBy(k => k).Select(k => Request.Form[k]).ToList();
                var startTimes = Request.Form.Keys.Where(k => k.StartsWith("StartTimes[")).OrderBy(k => k).Select(k => Request.Form[k]).ToList();

                int count = parts.Count;
                debugLog.AppendLine($"🧮 parts.Count = {parts.Count}, startTimes.Count = {startTimes.Count}<br/>");

                if (count == 0)
                {
                    TempData["Error"] += $"⚠️ No parts were received from the form.<br/>{debugLog}<br/>";
                    return RedirectToPage();
                }

                // Combine all rows into a single list and sort them by StartTime
                var records = new List<(int ID, string Part, string Assignee, string Assistant, TimeSpan StartTime)>();

                for (int i = 0; i < count; i++)
                {
                    int id = int.TryParse(partIds[i], out var parsedId) ? parsedId : 0;
                    string part = parts[i];
                    string assignee = assignees[i];
                    string assistant = assistants[i];
                    TimeSpan startTime = TimeSpan.TryParse(startTimes[i], out var parsedTime) ? parsedTime : TimeSpan.Zero;

                    records.Add((id, part, assignee, assistant, startTime));
                }

                // ✅ Sort by StartTime
                records = records.OrderBy(r => r.StartTime).ToList();

                using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                await connection.OpenAsync();

                int inserted = 0;

                if (DateTime.TryParse(Request.Form["Date"], out var meetingDate))
                {
                    using var dateCmd = new SqlCommand("SaveMidweekDate", connection)
                    {
                        CommandType = CommandType.StoredProcedure
                    };

                    dateCmd.Parameters.AddWithValue("@CongID", CongregationID);
                    dateCmd.Parameters.AddWithValue("@WeekOf", SelectedMeetingWeekText);
                    dateCmd.Parameters.AddWithValue("@Date", meetingDate);

                    await dateCmd.ExecuteNonQueryAsync();
                    debugLog.AppendLine($"📅 Saved Date {meetingDate:yyyy-MM-dd} for WeekOf '{SelectedMeetingWeekText}'");
                }
                else
                {
                    debugLog.AppendLine("⚠️ Invalid or missing Date field.");
                }

                for (int i = 0; i < records.Count; i++)
                {
                    var r = records[i];

                    debugLog.AppendLine($"🟢 DB Payload [{i}]: ID={r.ID}, Part='{r.Part}', Assignee='{r.Assignee}', Assistant='{r.Assistant}', StartTime={r.StartTime}<br/>");

                    try
                    {
                        using var cmd = new SqlCommand("SaveOCLMParts", connection)
                        {
                            CommandType = CommandType.StoredProcedure
                        };

                        cmd.Parameters.AddWithValue("@ID", r.ID);
                        cmd.Parameters.AddWithValue("@CongID", CongregationID);
                        cmd.Parameters.AddWithValue("@WeekOf", SelectedMeetingWeekText);
                        cmd.Parameters.AddWithValue("@StartTime", r.StartTime);
                        cmd.Parameters.AddWithValue("@Part", string.IsNullOrWhiteSpace(r.Part) ? "_" : r.Part);
                        cmd.Parameters.AddWithValue("@Assignee", string.IsNullOrWhiteSpace(r.Assignee) ? DBNull.Value : r.Assignee);
                        cmd.Parameters.AddWithValue("@Assistant", string.IsNullOrWhiteSpace(r.Assistant) ? DBNull.Value : r.Assistant);

                        int rows = await cmd.ExecuteNonQueryAsync();
                        string operation = r.ID > 0 ? "📝 UPDATE" : "➕ INSERT";
                        debugLog.AppendLine($"{operation} | Rows affected: {rows}<br/>");

                        inserted += rows;
                    }
                    catch (Exception ex)
                    {
                        debugLog.AppendLine($"❌ DB Error on row {i}: {ex.Message}<br/>");
                    }
                }

                if (inserted > 0)
                {
                    TempData["Success"] = $"✅ {inserted} assignment(s) saved successfully.<br/><hr/>{debugLog}";
                }
                else
                {
                    TempData["Error"] += $"⚠️ No data was saved.<br/><hr/>{debugLog}";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] += $"❌ Critical error: {ex.Message}<br/>";
            }

            return RedirectToPage();
        }
    }
}
