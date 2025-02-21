using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using Khronos4.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Khronos4.Data;

namespace Khronos4.Pages
{
    public class PlacementsModel : PageModel
    {
        public string CurrentMonth { get; set; }
        public int CurrentYear { get; set; }
        public int CurrentMonthNumber { get; set; } // Store month as a number
        public string UserFullName { get; set; }
        public Dictionary<DateTime, PlacementData> PlacementEntries { get; set; } = new();

        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;

        public PlacementsModel(IConfiguration configuration, AppDbContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        public class PlacementData
        {
            public decimal Hours { get; set; }
            public int Placements { get; set; }
            public int RVs { get; set; }
            public int BS { get; set; }
            public decimal LDC { get; set; }
            public string Notes { get; set; }
        }

        public async Task OnGetAsync()
        {
            CurrentYear = DateTime.Now.Year;
            CurrentMonthNumber = DateTime.Now.Month;
            CurrentMonth = DateTime.Now.ToString("MMMM", CultureInfo.InvariantCulture);

            // Ensure UserFullName is properly set
            UserFullName = (User.Claims.FirstOrDefault(c => c.Type == "FullName")?.Value ?? "Unknown User").Trim();

            // Call the stored procedure to ensure all days exist
            await GenerateMonthlyPlacements(UserFullName, CurrentYear, CurrentMonthNumber);

            // Load placements after ensuring they exist
            await LoadPlacementData(UserFullName, CurrentYear, CurrentMonthNumber);
        }

        public async Task<IActionResult> OnPostUpdatePlacementAsync(DateTime date)
        {
            try
            {
                UserFullName = (User?.Identity?.IsAuthenticated == true)
                    ? User.Claims.FirstOrDefault(c => c.Type == "FullName")?.Value ?? "Unknown User"
                    : "Unknown User";

                if (string.IsNullOrWhiteSpace(UserFullName) || UserFullName == "Unknown User")
                {
                    return Content("⚠️ Error: User not recognized.", "text/html");
                }

                // Generate unique field names per date
                var dateStr = date.ToString("yyyy-MM-dd");

                // Retrieve values using unique names
                var hoursStr = Request.Form[$"hours_{dateStr}"].ToString();
                var placementsStr = Request.Form[$"placements_{dateStr}"].ToString();
                var rvsStr = Request.Form[$"rvs_{dateStr}"].ToString();
                var bsStr = Request.Form[$"bs_{dateStr}"].ToString();
                var ldcStr = Request.Form[$"ldc_{dateStr}"].ToString();
                var notes = Request.Form[$"notes_{dateStr}"].ToString();

                // If notes is empty, set it to null
                if (string.IsNullOrWhiteSpace(notes))
                {
                    notes = null;
                }

                // Convert to correct types
                if (!decimal.TryParse(hoursStr, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal parsedHours))
                    return Content($"⚠️ Error: Invalid hours value received: {hoursStr}", "text/html");

                if (!int.TryParse(placementsStr, out int parsedPlacements))
                    return Content($"⚠️ Error: Invalid placements value received: {placementsStr}", "text/html");

                if (!int.TryParse(rvsStr, out int parsedRVs))
                    return Content($"⚠️ Error: Invalid return visits value received: {rvsStr}", "text/html");

                if (!int.TryParse(bsStr, out int parsedBS))
                    return Content($"⚠️ Error: Invalid bible studies value received: {bsStr}", "text/html");

                if (!decimal.TryParse(ldcStr, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal parsedLDC))
                    parsedLDC = 0m; // Default if blank

                // Debugging output
                var debugMessage = $@"
                    <h3>🛠 Debugging Information</h3>
                    <ul>
                        <li><strong>Owner:</strong> {UserFullName}</li>
                        <li><strong>Date:</strong> {date}</li>
                        <li><strong>Hours:</strong> {parsedHours}</li>
                        <li><strong>Placements:</strong> {parsedPlacements}</li>
                        <li><strong>Return Visits:</strong> {parsedRVs}</li>
                        <li><strong>Bible Studies:</strong> {parsedBS}</li>
                        <li><strong>LDC:</strong> {parsedLDC}</li>
                        <li><strong>Notes:</strong> {notes ?? "(empty)"}</li>
                    </ul>
                ";

                // Execute update
                var parameters = new[]
                {
                    new SqlParameter("@Owner", SqlDbType.NVarChar) { Value = UserFullName },
                    new SqlParameter("@Date", SqlDbType.Date) { Value = date },
                    new SqlParameter("@Hours", SqlDbType.Decimal) { Value = parsedHours },
                    new SqlParameter("@Placements", SqlDbType.Int) { Value = parsedPlacements },
                    new SqlParameter("@RVs", SqlDbType.Int) { Value = parsedRVs },
                    new SqlParameter("@BS", SqlDbType.Int) { Value = parsedBS },
                    new SqlParameter("@LDC", SqlDbType.Decimal) { Value = parsedLDC },
                    new SqlParameter("@Notes", SqlDbType.NVarChar) { Value = string.IsNullOrWhiteSpace(notes) ? (object)DBNull.Value : notes }
                };

                await _context.Database.ExecuteSqlRawAsync("EXEC [dbo].[UpdatePlacement] @Owner, @Date, @Hours, @Placements, @RVs, @BS, @LDC, @Notes", parameters);

                return Content($"✅ <h2>Success! Placement updated.</h2><br>{debugMessage}", "text/html");
            }
            catch (Exception ex)
            {
                return Content($"<h2>🚨 Error in UpdatePlacement</h2><p>{ex.Message}</p><pre>{ex.StackTrace}</pre>", "text/html");
            }
        }

        private async Task LoadPlacementData(string owner, int year, int month)
        {
            PlacementEntries = new Dictionary<DateTime, PlacementData>();

            var parameters = new[]
            {
        new SqlParameter("@Owner", SqlDbType.NVarChar) { Value = owner },
        new SqlParameter("@Year", SqlDbType.Int) { Value = year },
        new SqlParameter("@Month", SqlDbType.Int) { Value = month }
    };

            var placements = await _context.Placements
                .FromSqlRaw("EXEC [dbo].[GetMonthlyPlacements] @Owner, @Year, @Month", parameters)
                .ToListAsync();

            foreach (var placement in placements)
            {
                PlacementEntries[placement.Date] = new PlacementData
                {
                    Hours = placement.Hours ?? 0m,
                    Placements = placement.Placements ?? 0,
                    RVs = placement.RVs ?? 0,
                    BS = placement.BS ?? 0,
                    LDC = placement.LDC ?? 0m,
                    Notes = placement.Notes ?? ""
                };
            }
        }

        private async Task GenerateMonthlyPlacements(string owner, int year, int month)
        {
            var parameters = new[]
            {
                new SqlParameter("@Owner", SqlDbType.NVarChar) { Value = owner },
                new SqlParameter("@Year", SqlDbType.Int) { Value = year },
                new SqlParameter("@Month", SqlDbType.Int) { Value = month }
            };

            await _context.Database.ExecuteSqlRawAsync("EXEC [dbo].[GenerateMonthlyPlacements] @Owner, @Year, @Month", parameters);
        }
    }
}
