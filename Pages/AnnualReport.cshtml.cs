using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Khronos4.Data;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Khronos4.Models;
using Microsoft.AspNetCore.Authorization;

namespace Khronos4.Pages
{
    [Authorize]
    public class AnnualReportModel : PageModel
    {
        private readonly AppDbContext _context;
        public string UserFullName { get; set; }
        public int ServiceYearStart { get; set; }
        public int ServiceYearEnd { get; set; }
        public List<MonthlySummary> MonthlySummaries { get; set; } = new();

        // Store Total Yearly Values
        public decimal TotalYearlyHours { get; set; }
        public int TotalYearlyPlacements { get; set; }
        public int TotalYearlyRVs { get; set; }
        public int TotalYearlyBS { get; set; }
        public decimal TotalYearlyLDC { get; set; }

        public AnnualReportModel(AppDbContext context)
        {
            _context = context;
        }

        public async Task OnGetAsync()
        {
            var user = HttpContext.User;
            UserFullName = user.Claims.FirstOrDefault(c => c.Type == "FullName")?.Value ?? "Unknown User";

            // Determine current JW Service Year
            DateTime today = DateTime.Today;
            if (today.Month >= 9)  // If September or later, service year starts this year
            {
                ServiceYearStart = today.Year;
                ServiceYearEnd = today.Year + 1;
            }
            else  // If before September, service year started last year
            {
                ServiceYearStart = today.Year - 1;
                ServiceYearEnd = today.Year;
            }

            // Get monthly data for JW service year (Sept - Aug)
            MonthlySummaries = new List<MonthlySummary>();
            for (int i = 0; i < 12; i++)
            {
                int month = ((9 + i - 1) % 12) + 1; // Start from Sept (9) to Aug (8)
                int year = (month >= 9) ? ServiceYearStart : ServiceYearEnd;

                var summary = await GetMonthlyTotals(UserFullName, year, month);
                MonthlySummaries.Add(new MonthlySummary
                {
                    MonthName = new DateTime(year, month, 1).ToString("MMMM yyyy", CultureInfo.InvariantCulture),
                    Hours = summary.Hours,
                    Placements = summary.Placements,
                    RVs = summary.RVs,
                    BS = summary.BS,
                    LDC = summary.LDC
                });

                // Update yearly totals
                TotalYearlyHours += summary.Hours;
                TotalYearlyPlacements += summary.Placements;
                TotalYearlyRVs += summary.RVs;
                TotalYearlyBS += summary.BS;
                TotalYearlyLDC += summary.LDC;
            }
        }

        private async Task<MonthlySummary> GetMonthlyTotals(string owner, int year, int month)
        {
            var parameters = new[]
            {
                new SqlParameter("@Owner", System.Data.SqlDbType.NVarChar) { Value = owner },
                new SqlParameter("@Year", System.Data.SqlDbType.Int) { Value = year },
                new SqlParameter("@Month", System.Data.SqlDbType.Int) { Value = month }
            };

            var results = await _context.Placements
                .FromSqlRaw("EXEC [dbo].[GetMonthlyPlacements] @Owner, @Year, @Month", parameters)
                .ToListAsync();

            return new MonthlySummary
            {
                Hours = results.Sum(x => x.Hours ?? 0m),  // Handle null decimals
                Placements = results.Sum(x => x.Placements ?? 0), // Handle null ints
                RVs = results.Sum(x => x.RVs ?? 0),
                BS = results.Sum(x => x.BS ?? 0),
                LDC = results.Sum(x => x.LDC ?? 0m)
            };
        }

        public class MonthlySummary
        {
            public string MonthName { get; set; }
            public decimal Hours { get; set; }
            public int Placements { get; set; }
            public int RVs { get; set; }
            public int BS { get; set; }
            public decimal LDC { get; set; }
        }
    }
}
