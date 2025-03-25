using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Khronos4.Data;
using Khronos4.Models;

namespace Khronos4.Pages
{
    public class CongregationManagerModel : BasePageModel
    {
        public CongregationManagerModel(AppDbContext context) : base(context) { }

        [BindProperty]
        public Congregation CongregationDetails { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                AddDebugMessage($"🔍 Fetching congregation details for CongregationID: {CongregationID}");

                // Fetch congregation details using EF Core
                CongregationDetails = await _context.Congregations
                    .FirstOrDefaultAsync(c => c.CongID == CongregationID);

                if (CongregationDetails == null)
                {
                    AddDebugMessage("⚠️ No congregation found. Creating an empty model.");
                    CongregationDetails = new Congregation(); // Ensure the form does not break
                }
                else
                {
                    AddDebugMessage("✅ Congregation details loaded successfully.");
                }

                return Page();
            }
            catch (Exception ex)
            {
                AddDebugMessage($"❌ Error in OnGetAsync(): {ex.Message}");
                return Page();
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                using (var connection = new SqlConnection(_context.Database.GetConnectionString()))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("dbo.UpdateCongregationDetails", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        // Required Fields
                        command.Parameters.AddWithValue("@CongID", CongregationDetails.CongID);
                        command.Parameters.AddWithValue("@Name", CongregationDetails.Name ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Language", CongregationDetails.Language ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@Address", CongregationDetails.Address ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@City", CongregationDetails.City ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@State", CongregationDetails.State ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@ZipCode", CongregationDetails.ZipCode ?? (object)DBNull.Value); // New Field
                        command.Parameters.AddWithValue("@Country", CongregationDetails.Country ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@CongCOBE", CongregationDetails.CongCOBE ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@CongSect", CongregationDetails.CongSect ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@CongSO", CongregationDetails.CongSO ?? (object)DBNull.Value);

                        // Midweek and Weekend Changes
                        command.Parameters.AddWithValue("@MidweekDate", CongregationDetails.MidweekDate ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@MidweekTime", CongregationDetails.MidweekTime ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@WeekendDate", CongregationDetails.WeekendDate ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@WeekendTime", CongregationDetails.WeekendTime ?? (object)DBNull.Value);

                        // Circuit
                        command.Parameters.AddWithValue("@Circuit", CongregationDetails.Circuit ?? (object)DBNull.Value);

                        await command.ExecuteNonQueryAsync();
                    }
                }

                // ✅ Explicitly reload congregation details after update
                CongregationDetails = await _context.Congregations.FirstOrDefaultAsync(c => c.CongID == CongregationID);

                TempData["SuccessMessage"] = "✅ Congregation Info Updated!";

                // ✅ Redirect to reload the page and trigger BasePageModel to fetch updated data
                return Page();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"❌ Error updating congregation: {ex.Message}";
                return Page();
            }
        }

        private void AddDebugMessage(string message)
        {
            List<string> messages;

            // Ensure TempData["DebugError"] exists and is a valid JSON list
            if (TempData.ContainsKey("DebugError") && TempData["DebugError"] is string existingMessages)
            {
                try
                {
                    messages = Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(existingMessages) ?? new List<string>();
                }
                catch (Newtonsoft.Json.JsonReaderException)
                {
                    messages = new List<string>(); // Reset if parsing fails
                }
            }
            else
            {
                messages = new List<string>();
            }

            // Add new message and store it in TempData
            messages.Add(message);
            TempData["DebugError"] = Newtonsoft.Json.JsonConvert.SerializeObject(messages);
        }
    }
}
