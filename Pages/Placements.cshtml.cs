using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using Khronos4.Models;

namespace Khronos4.Pages
{
    public class PlacementsModel : PageModel
    {
        public string UserFullName { get; set; }

        private readonly IConfiguration _configuration;

        public PlacementsModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string CurrentMonth { get; set; }
        public int CurrentYear { get; set; }
        public Dictionary<int, PlacementData> PlacementEntries { get; set; } = new();

        public async Task OnGetAsync()
        {
            DateTime today = DateTime.Now;
            CurrentMonth = today.ToString("MMMM", CultureInfo.InvariantCulture);
            CurrentYear = today.Year;
            UserFullName = User.Claims.FirstOrDefault(c => c.Type == "FullName")?.Value ?? "Unknown User";
            await LoadPlacementData();
        }

        private async Task LoadPlacementData()
        {
            var placements = new Dictionary<int, PlacementData>();

            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "dbo.GetPlacements";
                    command.CommandType = CommandType.StoredProcedure;

                    // Ensure Owner is passed as NVARCHAR
                    command.Parameters.Add(new SqlParameter("@Owner", SqlDbType.NVarChar) { Value = UserFullName });

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var day = reader.GetInt32(0);  // Assuming first column is "day" (integer)
                            placements[day] = new PlacementData
                            {
                                Hours = reader.IsDBNull(1) ? 0m : Convert.ToDecimal(reader.GetValue(1)),
                                Placements = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
                                RVs = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
                                BS = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                                LDC = reader.IsDBNull(5) ? 0m : Convert.ToDecimal(reader.GetValue(5)),
                                Notes = reader.IsDBNull(6) ? "" : reader.GetString(6)
                            };
                        }
                    }
                }
            }

            PlacementEntries = placements;
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
