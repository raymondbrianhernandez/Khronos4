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
using static System.Net.Mime.MediaTypeNames;
using Microsoft.AspNetCore.Authorization;

namespace Khronos4.Pages
{
    [Authorize]
    public class PlacementsModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public string UserFullName { get; set; }
        public int SelectedMonth { get; set; }
        public string SelectedMonthName { get; set; }
        public int SelectedYear { get; set; }
        public List<int> AvailableYears { get; set; } = new();
        public Dictionary<DateTime, PlacementData> PlacementEntries { get; set; } = new();
        public bool HasRecords { get; set; } = true;
        public string CurrentMonth { get; set; }
        public int CurrentYear { get; set; }
        public int CurrentMonthNumber { get; set; } // Store month as a number

        public PlacementsModel(IConfiguration configuration, AppDbContext context)
        {
            _configuration = configuration;
            _context = context;
        }

        public async Task OnGetAsync(int? month, int? year)
        {
            UserFullName = (User.Claims.FirstOrDefault(c => c.Type == "FullName")?.Value ?? "Unknown User").Trim();

            // Load available years dynamically based on placement records
            AvailableYears = await _context.Placements
                .Where(p => p.Owner == UserFullName)
                .Select(p => p.Date.Year)
                .Distinct()
                .OrderByDescending(y => y)
                .ToListAsync();

            // Default to the most recent year if no selection is made
            SelectedYear = year ?? (AvailableYears.Any() ? AvailableYears.First() : DateTime.Now.Year);
            SelectedMonth = month ?? DateTime.Now.Month;

            SelectedMonthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(SelectedMonth);

            // Load placements for the selected month and year
            await LoadPlacementData(UserFullName, SelectedYear, SelectedMonth);
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
                    return RedirectToPage("Placements", new { error = "User not recognized." });
                }

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

                // Convert values safely
                if (!decimal.TryParse(hoursStr, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal parsedHours) ||
                    !int.TryParse(placementsStr, out int parsedPlacements) ||
                    !int.TryParse(rvsStr, out int parsedRVs) ||
                    !int.TryParse(bsStr, out int parsedBS) ||
                    !decimal.TryParse(ldcStr, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal parsedLDC))
                {
                    return RedirectToPage("Placements", new { error = "Invalid input values." });
                }

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

                int rowsAffected = await _context.Database.ExecuteSqlRawAsync(
                           "EXEC [dbo].[UpdatePlacement] @Owner, @Date, @Hours, @Placements, @RVs, @BS, @LDC, @Notes",
                           parameters);

                if (rowsAffected > 0)
                {
                    return RedirectToPage("/Placements", new { success = 1 });
                }
                else
                {
                    return RedirectToPage("/Placements", new { error = 1 });
                }
            }
            catch (Exception ex)
            {
                return RedirectToPage("Placements", new { error = "An error occurred while updating." });
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

            if (!placements.Any())
            {
                HasRecords = false;
                return;
            }

            HasRecords = true;

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

        public class PlacementData
        {
            public decimal Hours { get; set; }
            public int Placements { get; set; }
            public int RVs { get; set; }
            public int BS { get; set; }
            public decimal LDC { get; set; }
            public string Notes { get; set; }
        }
    }
}
