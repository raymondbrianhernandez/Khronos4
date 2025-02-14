using Khronos4.Data;
using Khronos4.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Khronos4.Pages
{
    public class H2HRecordsModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<H2HRecordsModel> _logger;

        // Bind properties for input fields
        [BindProperty] public string Name { get; set; }
        [BindProperty] public string Address { get; set; }
        [BindProperty] public string City { get; set; }
        [BindProperty] public string Province { get; set; }
        [BindProperty] public string Postal_Code { get; set; }
        [BindProperty] public string Country { get; set; }
        [BindProperty] public string Language { get; set; }
        [BindProperty] public string Telephone { get; set; }
        [BindProperty] public string Notes { get; set; }
        [BindProperty] public string Status { get; set; }

        public List<Householder> Householders { get; set; } = new List<Householder>();
        public string ErrorMessage { get; set; }

        // API keys stored in configuration (via user secrets or environment variables)
        public string GoogleApiKey { get; }
        public string MapLibreApiKey { get; }

        public IEnumerable<SelectListItem> StatusOptions { get; set; }
        public IEnumerable<SelectListItem> LanguageOptions { get; set; }

        public H2HRecordsModel(
            AppDbContext context,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<H2HRecordsModel> logger)
        {
            _context = context;
            _httpClient = httpClientFactory.CreateClient();
            _configuration = configuration;
            _logger = logger;

            GoogleApiKey = _configuration["GoogleApiKey"];
            MapLibreApiKey = _configuration["MapLibreApiKey"];
        }

        // ==============================
        // OnGet: Display Householders
        // ==============================
        public async Task<IActionResult> OnGetAsync()
        {
            // Populate dropdown options for Status
            StatusOptions = new List<SelectListItem>
            {
                new SelectListItem { Value = "Interested",   Text = "Interested" },
                new SelectListItem { Value = "Not Home",     Text = "Not Home" },
                new SelectListItem { Value = "Do Not Return",Text = "Do Not Return" }
            };

            // Populate dropdown options for Language
            LanguageOptions = new List<SelectListItem>
            {
                new SelectListItem { Value = "Tagalog", Text = "Tagalog" },
                new SelectListItem { Value = "English", Text = "English" },
                new SelectListItem { Value = "Spanish", Text = "Spanish" },
                new SelectListItem { Value = "Japanese", Text = "Japanese" }
            };

            // Check if user is logged in
            string userFullName = User.Claims.FirstOrDefault(c => c.Type == "FullName")?.Value ?? "";
            if (string.IsNullOrEmpty(userFullName))
            {
                return RedirectToPage("/Login");
            }

            // Load householders for the logged-in user
            Householders = await LoadHouseholdersFromDatabase(userFullName);
            return Page();
        }

        // ==============================
        // CREATE: OnPost (Add a New Record)
        // ==============================
        public async Task<IActionResult> OnPostAsync()
        {
            // Validate required fields
            if (string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(Address) || string.IsNullOrEmpty(City))
            {
                ErrorMessage = "Name, Address, and City are required.";
                return Page();
            }

            try
            {
                // Retrieve the logged-in user's full name from claims
                string userFullName = User.Claims.FirstOrDefault(c => c.Type == "FullName")?.Value ?? "";

                // Use the Google Geocoding API to get GPS coordinates for the provided address
                var (latitude, longitude, fullAddress, buildingType) = await GetCoordinatesAsync(Address);

                DateTime createdAt = DateTime.UtcNow;

                // Build a new SQL connection using the connection string from configuration
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "dbo.InsertHouseholder";
                        command.CommandType = System.Data.CommandType.StoredProcedure;

                        // Add parameters to match your stored procedure:
                        command.Parameters.AddRange(new SqlParameter[]
                        {
                    new SqlParameter("@Owner", userFullName),
                    new SqlParameter("@Name", Name),
                    new SqlParameter("@Address", Address),
                    new SqlParameter("@City", City),
                    new SqlParameter("@Province", Province),
                    new SqlParameter("@Postal_Code", Postal_Code),
                    new SqlParameter("@Country", Country),
                    new SqlParameter("@Telephone", string.IsNullOrEmpty(Telephone) ? (object)DBNull.Value : SanitizeTelephone(Telephone)),
                    new SqlParameter("@Notes", string.IsNullOrEmpty(Notes) ? (object)DBNull.Value : Notes),
                    new SqlParameter("@Notes_Private", DBNull.Value), // If not used, send DBNull.Value
                    new SqlParameter("@Status", Status),
                    new SqlParameter("@Language", Language),
                    new SqlParameter("@Latitude", latitude),
                    new SqlParameter("@Longitude", longitude),
                    new SqlParameter("@CreatedAt", createdAt)
                        });

                        await command.ExecuteNonQueryAsync();
                    }
                }

                TempData["SuccessMessage"] = "✅ New householder added successfully!";
                return RedirectToPage("H2HRecords");
            }
            catch (SqlException ex)
            {
                ErrorMessage = $"❌ SQL Error {ex.Number}: {ex.Message}";
                _logger.LogError($"SQL Error {ex.Number}: {ex.Message}");
                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"❌ Unexpected Error: {ex.Message}";
                _logger.LogError($"Unexpected Error: {ex.Message}");
                return Page();
            }
        }

        // ==============================
        // EDIT: OnPostEditAsync (Update an Existing Record)
        // Form posts to ?handler=Edit
        // ==============================
        public async Task<IActionResult> OnPostEditAsync()
        {
            int id = Convert.ToInt32(Request.Form["Id"]);
            string updatedName = Request.Form["Name"];
            string updatedAddress = Request.Form["Address"];
            string updatedCity = Request.Form["City"];
            string updatedProvince = Request.Form["Province"];
            string updatedPostalCode = Request.Form["Postal_Code"];
            string updatedCountry = Request.Form["Country"];
            string updatedTelephone = Request.Form["Telephone"];
            string updatedLanguage = Request.Form["Language"];
            string updatedNotes = Request.Form["Notes"];
            string updatedStatus = Request.Form["Status"];

            if (string.IsNullOrEmpty(updatedName) || string.IsNullOrEmpty(updatedAddress) || string.IsNullOrEmpty(updatedCity))
            {
                ErrorMessage = "Name, Address, and City cannot be empty.";
                return await OnGetAsync();
            }

            try
            {
                // For simplicity, always re-run geocoding for the updated address.
                var (latitude, longitude, _, _) = await GetCoordinatesAsync(updatedAddress);

                // Use a new SQL connection from configuration.
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "dbo.UpdateHouseholder";
                        command.CommandType = System.Data.CommandType.StoredProcedure;

                        command.Parameters.AddRange(new SqlParameter[]
                        {
                            new SqlParameter("@Id", id),
                            new SqlParameter("@Name", updatedName),
                            new SqlParameter("@Address", updatedAddress),
                            new SqlParameter("@City", updatedCity),
                            new SqlParameter("@Province", updatedProvince),
                            new SqlParameter("@Postal_Code", updatedPostalCode),
                            new SqlParameter("@Country", updatedCountry),
                            new SqlParameter("@Telephone", string.IsNullOrEmpty(updatedTelephone) ? (object)DBNull.Value : SanitizeTelephone(updatedTelephone)),
                            new SqlParameter("@Language", updatedLanguage),
                            new SqlParameter("@Notes", string.IsNullOrEmpty(updatedNotes) ? (object)DBNull.Value : updatedNotes),
                            new SqlParameter("@Status", updatedStatus),
                            new SqlParameter("@Latitude", latitude),
                            new SqlParameter("@Longitude", longitude)
                        });

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        if (rowsAffected > 0)
                        {
                            TempData["SuccessMessage"] = "✅ Householder updated successfully!";
                        }
                        else
                        {
                            TempData["SuccessMessage"] = "⚠️ No records were updated.";
                        }
                    }
                }

                return RedirectToPage("H2HRecords");
            }
            catch (SqlException ex)
            {
                ErrorMessage = $"❌ SQL Error {ex.Number}: {ex.Message}";
                _logger.LogError($"SQL Error {ex.Number}: {ex.Message}");
                return await OnGetAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"❌ Unexpected Error: {ex.Message}";
                _logger.LogError($"Unexpected Error: {ex.Message}");
                return await OnGetAsync();
            }
        }

        // ==============================
        // DELETE: OnGetDeleteAsync (Delete a Record)
        // Called by /H2HRecords?handler=Delete&id=xxx
        // ==============================
        public async Task<IActionResult> OnGetDeleteAsync(int id)
        {
            try
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "dbo.DeleteHouseholder";
                        command.CommandType = System.Data.CommandType.StoredProcedure;
                        command.Parameters.Add(new SqlParameter("@Id", id));

                        int rowsAffected = await command.ExecuteNonQueryAsync();
                        if (rowsAffected > 0)
                        {
                            TempData["SuccessMessage"] = "✅ Householder deleted successfully!";
                        }
                        else
                        {
                            TempData["SuccessMessage"] = "⚠️ No records were deleted.";
                        }
                    }
                }
                return RedirectToPage("H2HRecords");
            }
            catch (SqlException ex)
            {
                ErrorMessage = $"❌ SQL Error {ex.Number}: {ex.Message}";
                _logger.LogError($"SQL Error {ex.Number}: {ex.Message}");
                return await OnGetAsync();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"❌ Unexpected Error: {ex.Message}";
                _logger.LogError($"Unexpected Error: {ex.Message}");
                return await OnGetAsync();
            }
        }

        // ==============================
        // Helper Methods
        // ==============================
        private async Task<List<Householder>> LoadHouseholdersFromDatabase(string owner)
        {
            var householders = new List<Householder>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            using (var connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "dbo.GetHouseholders";
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.Add(new SqlParameter("@Owner", owner));

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            householders.Add(new Householder
                            {
                                Id = reader.GetInt32(0),                                      // Address_ID
                                Name = reader.GetString(1),                                   // Name
                                Address = reader.GetString(2),                                // Address
                                Suite = reader.IsDBNull(3) ? null : reader.GetString(3),      // Suite
                                City = reader.GetString(4),                                   // City
                                Province = reader.GetString(5),                               // Province
                                Postal_Code = reader.GetString(6),                            // Postal_Code
                                Country = reader.GetString(7),                                // Country
                                Telephone = reader.IsDBNull(8) ? null : reader.GetString(8),  // Telephone
                                Notes = reader.IsDBNull(9) ? null : reader.GetString(9),      // Notes
                                Status = reader.IsDBNull(10) ? null : reader.GetString(10),    // Status
                                Latitude = reader.IsDBNull(11) ? 0 : Convert.ToDecimal(reader.GetValue(11)), // Latitude
                                Longitude = reader.IsDBNull(12) ? 0 : Convert.ToDecimal(reader.GetValue(12)),// Longitude
                                CreatedAt = reader.GetDateTime(13),                           // CreatedAt
                                Language = reader.IsDBNull(14) ? null : reader.GetString(14)   // Language
                            });
                        }
                    }
                }
            }
            return householders;
        }

        private string SanitizeTelephone(string telephone)
        {
            if (string.IsNullOrEmpty(telephone))
            {
                return telephone;
            }
            // Filter out non-digit characters
            return new string(telephone.Where(char.IsDigit).ToArray());
        }

        private async Task<(decimal Latitude, decimal Longitude, string FullAddress, string BuildingType)> GetCoordinatesAsync(string address)
        {
            string apiKey = GoogleApiKey;
            string formattedAddress = Uri.EscapeDataString(address);
            string url = $"https://maps.googleapis.com/maps/api/geocode/json?address={formattedAddress}&key={apiKey}";

            try
            {
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"❌ Google API Error: {response.StatusCode}");
                    return (0, 0, "Unknown", "Unknown");
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                _logger.LogInformation($"📡 API Response: {responseBody}");

                using (JsonDocument doc = JsonDocument.Parse(responseBody))
                {
                    if (!doc.RootElement.TryGetProperty("results", out var results) || results.GetArrayLength() == 0)
                    {
                        _logger.LogWarning("⚠️ No results found in Google API response.");
                        return (0, 0, "Unknown", "Unknown");
                    }

                    var firstResult = results[0];
                    decimal lat = firstResult.GetProperty("geometry").GetProperty("location").GetProperty("lat").GetDecimal();
                    decimal lng = firstResult.GetProperty("geometry").GetProperty("location").GetProperty("lng").GetDecimal();
                    string fullAddress = firstResult.GetProperty("formatted_address").GetString();
                    string buildingType = firstResult.TryGetProperty("types", out var types) && types.GetArrayLength() > 0
                        ? types[0].GetString()
                        : "Unknown";

                    _logger.LogInformation($"📍 Extracted Coordinates: Lat={lat}, Lng={lng}");
                    return (lat, lng, fullAddress, buildingType);
                }
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError($"❌ JSON Parsing Error: {jsonEx.Message}");
                return (0, 0, "Unknown", "Unknown");
            }
            catch (Exception ex)
            {
                _logger.LogError($"❌ Unexpected Error in GetCoordinatesAsync: {ex.Message}");
                return (0, 0, "Unknown", "Unknown");
            }
        }
    }
}
