using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using Khronos4.Models;
using Microsoft.AspNetCore.Authorization;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Khronos4.Data;
using System.Formats.Asn1;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using System.IO;
using System.Linq;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;
using System.IO;
using DocumentFormat.OpenXml;


namespace Khronos4.Pages
{
    [Authorize]
    public class PublisherManagerModel : BasePageModel
    {
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;

        public PublisherManagerModel(IConfiguration configuration, AppDbContext context) : base(context)
        {
            _configuration = configuration;
            _context = context;
            ServiceGroupNumbers = new List<int>();
        }

        [BindProperty]
        public string Congregation { get; set; }
        [BindProperty]
        public Publisher Publisher { get; set; } = new Publisher();
        
        public List<CSVPublisher> BulkUploadPreview { get; set; } = new List<CSVPublisher>();
        public List<Publisher> PublisherList { get; set; } = new List<Publisher>();
        public List<SelectListItem> Congregations { get; set; }
        public List<int> ServiceGroupNumbers { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadPublishers();
            LoadUserDetails();

            Congregations = new List<SelectListItem>();

            using (var connection = _context.Database.GetDbConnection())
            {
                await connection.OpenAsync();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "dbo.GetCongregations";
                    command.CommandType = System.Data.CommandType.StoredProcedure;

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            Congregations.Add(new SelectListItem
                            {
                                Value = reader["CongID"].ToString(),
                                Text = reader["Name"].ToString()
                            });
                        }
                    }
                }
            }

            return Page();
        }

        private async Task LoadServiceGroupNumbers()
        {
            ServiceGroupNumbers.Clear();
            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await conn.OpenAsync();
                using (SqlCommand cmd = new SqlCommand("GetDistinctServiceGroups", conn)) //Replace with your stored procedure or query.
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            ServiceGroupNumbers.Add(reader.GetInt32(0)); // Assuming the first column is the ServiceGroup number
                        }
                    }
                }
            }
        }

        private async Task LoadPublishers()
        {
            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                try
                {
                    await conn.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand("GetPublishersByCongregation", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        int userCongregationId = Convert.ToInt32(User.Claims.FirstOrDefault(c => c.Type == "Congregation")?.Value ?? "0");
                        cmd.Parameters.AddWithValue("@CongregationId", userCongregationId);

                        TempData["Debug"] = $"Loading publishers for CongregationId: {userCongregationId}";

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            PublisherList.Clear();
                            if (reader.HasRows)
                            {
                                TempData["Debug"] += " | Data Found";

                                while (await reader.ReadAsync())
                                {
                                    try
                                    {
                                        int id = reader.GetInt32(0);
                                        string name = reader.IsDBNull(1) ? "" : reader.GetString(1);
                                        int congregationId = reader.GetInt32(2);
                                        int serviceGroup = reader.GetInt32(3);
                                        string privilege = reader.IsDBNull(4) ? "" : reader.GetString(4);

                                        bool isRP = reader.IsDBNull(5) ? false : reader.GetBoolean(5);
                                        bool isCBSOverseer = reader.IsDBNull(6) ? false : reader.GetBoolean(6);
                                        bool isCBSAssistant = reader.IsDBNull(7) ? false : reader.GetBoolean(7);

                                        string phoneNumber = reader.IsDBNull(8) ? null : reader.GetString(8);
                                        string email = reader.IsDBNull(9) ? null : reader.GetString(9);
                                        string status = reader.IsDBNull(10) ? "" : reader.GetString(10);
                                        string notes = reader.IsDBNull(11) || string.IsNullOrWhiteSpace(reader.GetString(11)) ? null : reader.GetString(11).Trim();

                                        PublisherList.Add(new Publisher
                                        {
                                            Id = id,
                                            Name = name,
                                            Congregation = congregationId,
                                            ServiceGroup = serviceGroup,
                                            Privilege = privilege,
                                            IsRP = isRP,
                                            IsCBSOverseer = isCBSOverseer,
                                            IsCBSAssistant = isCBSAssistant,
                                            PhoneNumber = phoneNumber,
                                            Email = email,
                                            Status = status,
                                            Notes = notes
                                        });
                                    }
                                    catch (Exception ex)
                                    {
                                        TempData["DebugError"] += $" | Error reading row: {ex.Message}";
                                    }
                                }

                                TempData["Debug"] += $" | Total Publishers Loaded: {PublisherList.Count}";
                            }
                            else
                            {
                                TempData["Debug"] += " | No Data Found";
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    TempData["Error"] = $"Database Error: {ex.Message}";
                }

            }
        }

        public async Task<IActionResult> OnPostAddPublisherAsync()
        {
            try
            {
                TempData["Debug"] = "Attempting to add a publisher...";

                // Get the logged-in user's Congregation ID
                var user = HttpContext.User;
                int userCongregationId = Convert.ToInt32(user.Claims.FirstOrDefault(c => c.Type == "Congregation")?.Value ?? "0");

                // Retrieve form data
                string name = Request.Form["Publisher.Name"].ToString().Trim();
                string phoneNumber = Request.Form["Publisher.PhoneNumber"].ToString().Trim();
                string email = Request.Form["Publisher.Email"].ToString().Trim();
                string privilege = Request.Form["Publisher.Privilege"].ToString().Trim();
                string notes = Request.Form["Publisher.Notes"].ToString().Trim();
                bool isRp = Publisher.IsRP; // This will get the boolean value from the dropdown.
                bool isCBSOverseer = Publisher.IsCBSOverseer;
                bool isCBSAssistant = Publisher.IsCBSAssistant;

                // Ensure ServiceGroup is an int
                int serviceGroup = int.TryParse(Request.Form["Publisher.ServiceGroup"], out int sg) ? sg : 0;

                // Debugging logs
                TempData["DebugFormValues"] = $"Name: {name}, Congregation: {userCongregationId}, ServiceGroup: {serviceGroup}, " +
                                                $"Privilege: '{privilege}', IsRP: {isRp}, Phone: {phoneNumber}, " +
                                                $"Email: {email}, Notes: {notes}";
                TempData["DebugIsRP"] = $"IsRP Value: {isRp}";

                // If Privilege is empty, log an error and exit
                if (string.IsNullOrWhiteSpace(privilege))
                {
                    TempData["Error"] = "Privilege is required.";
                    return RedirectToPage();
                }

                // Check if the publisher already exists
                if (await PublisherExists(name, userCongregationId.ToString()))
                {
                    TempData["Error"] = "Publisher already exists in the database.";
                    return RedirectToPage();
                }

                // Call stored procedure to add publisher
                await ManagePublisher(
                    "ADD",
                    null,
                    name,
                    userCongregationId.ToString(),
                    serviceGroup.ToString(),
                    privilege,
                    isRp,
                    isCBSOverseer,
                    isCBSAssistant,
                    phoneNumber,
                    email,
                    "Active",
                    notes
                );

                TempData["Message"] = "Publisher Added Successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error adding publisher: {ex.Message}";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEditPublisherAsync()
        {
            try
            {
                if (!Request.Form.ContainsKey("id") || string.IsNullOrWhiteSpace(Request.Form["id"]))
                {
                    TempData["Error"] = "Invalid request. Publisher ID is missing.";
                    return RedirectToPage();
                }

                string idValue = Request.Form["id"];
                if (!int.TryParse(idValue, out int id))
                {
                    TempData["Error"] = "Invalid Publisher ID format.";
                    return RedirectToPage();
                }

                string name = Request.Form["name"];
                string privilege = Request.Form["privilege"];
                string phoneNumber = Request.Form["phoneNumber"];
                /*phoneNumber = SanitizePhoneNumber(phoneNumber);*/ /*Will test soon for WhatsApp*/
                string status = Request.Form["status"];
                string notes = Request.Form["notes"];
                string email = Request.Form["email"];

                // Parse service group
                string serviceGroupValue = Request.Form["serviceGroup"];
                int serviceGroup = int.TryParse(serviceGroupValue, out int sg) ? sg : 0;

                bool isRp = Request.Form["isRp"] == "true";
                bool isCBSOverseer = Request.Form["isCBSOverseer"] == "true";
                bool isCBSAssistant = Request.Form["isCBSAssistant"] == "true";

                var user = HttpContext.User;
                string userCongregationId = user.Claims.FirstOrDefault(c => c.Type == "Congregation")?.Value;

                await ManagePublisher("EDIT", id, name, userCongregationId, serviceGroup.ToString(), privilege, isRp, isCBSOverseer, isCBSAssistant, phoneNumber, email, status, notes);

                TempData["Message"] = "Publisher Info Edited Successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error editing publisher: {ex.Message}";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeletePublisherAsync()
        {
            try
            {
                if (!Request.Form.ContainsKey("id") || string.IsNullOrWhiteSpace(Request.Form["id"]))
                {
                    TempData["Error"] = "Invalid request. Publisher ID is missing.";
                    return RedirectToPage();
                }

                string idValue = Request.Form["id"];
                if (!int.TryParse(idValue, out int id))
                {
                    TempData["Error"] = "Invalid Publisher ID format.";
                    return RedirectToPage();
                }

                await ManagePublisher("DELETE", id, null, null, null, null, false, false, false, null, null, null, null);
                TempData["Message"] = "Publisher Deleted Successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting publisher: {ex.Message}";
            }

            return RedirectToPage();
        }

        private async Task ManagePublisher(string action, int? id, string name, string congregation, string serviceGroup, string privilege, bool isRp, bool isCBSOverseer, bool isCBSAssistant, string phoneNumber, string email, string status, string notes)
        {
            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await conn.OpenAsync();
                using (SqlCommand cmd = new SqlCommand("ManagePublisher", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@Action", action);
                    cmd.Parameters.AddWithValue("@Id", (object)id ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Name", (object)name ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Congregation", (object)congregation ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ServiceGroup", (object)serviceGroup ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Privilege", string.IsNullOrWhiteSpace(privilege) ? "---" : privilege);
                    cmd.Parameters.AddWithValue("@IsRP", isRp);
                    cmd.Parameters.AddWithValue("@IsCBSOverseer", isCBSOverseer);  
                    cmd.Parameters.AddWithValue("@IsCBSAssistant", isCBSAssistant); 
                    cmd.Parameters.AddWithValue("@PhoneNumber", (object)phoneNumber ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Email", (object)email ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Status", (object)status ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Notes", (object)notes ?? DBNull.Value);

                    try
                    {
                        int rowsAffected = await cmd.ExecuteNonQueryAsync();
                        TempData["DebugManagePublisher"] = $"Stored Procedure Executed: Rows Affected={rowsAffected}";
                    }
                    catch (Exception ex)
                    {
                        TempData["Error"] = $"SQL Error: {ex.Message}";
                    }
                }
            }
        }

        private async Task<bool> PublisherExists(string name, string congregation)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await connection.OpenAsync();
                string sql = "SELECT COUNT(*) FROM Publishers WHERE Name = @Name AND Congregation = @Congregation";
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddWithValue("@Name", name);
                    command.Parameters.AddWithValue("@Congregation", congregation);
                    return (int)await command.ExecuteScalarAsync() > 0;
                }
            }
        }

        public async Task<IActionResult> OnPostBulkUploadAsync(IFormFile csvFile)
        {
            TempData["UploadMessage"] = null; // Clear previous messages

            if (csvFile == null || csvFile.Length == 0)
            {
                TempData["UploadMessage"] = "Please select a valid CSV file.";
                return RedirectToPage();
            }

            var records = new List<CSVPublisher>();

            try
            {
                using (var reader = new StreamReader(csvFile.OpenReadStream()))
                using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    IgnoreBlankLines = true,
                    MissingFieldFound = null  // ✅ FIX: Ignore missing fields
                }))
                {
                    records = csv.GetRecords<CSVPublisher>().ToList();
                }

                if (!records.Any())
                {
                    TempData["UploadMessage"] = "CSV file is empty.";
                    return RedirectToPage();
                }

                var previewList = new List<CSVPublisher>();
                foreach (var record in records)
                {
                    if (string.IsNullOrWhiteSpace(record.Name) ||
                        record.ServiceGroup <= 0 ||  // Ensure valid INT
                        string.IsNullOrWhiteSpace(record.Privilege))
                    {
                        TempData["UploadMessage"] = "Error: Name, ServiceGroup (integer), and Privilege are required in every row.";
                        return RedirectToPage();
                    }

                    record.PhoneNumber = string.IsNullOrWhiteSpace(record.PhoneNumber) ? null : record.PhoneNumber;
                    previewList.Add(record);
                }

                if (!previewList.Any())
                {
                    TempData["UploadMessage"] = "No valid records to preview.";
                    return RedirectToPage();
                }

                // 🔍 Debugging Log: Check Preview Count
                Console.WriteLine($"Preview Count: {previewList.Count}");

                // Store in session
                var jsonData = JsonSerializer.Serialize(previewList);
                HttpContext.Session.SetString("BulkUploadPreview", jsonData);
                Console.WriteLine($"Session Data Length: {jsonData.Length}");

                TempData["UploadMessage"] = "Preview generated. Click 'Confirm Upload' to insert.";
            }
            catch (Exception ex)
            {
                TempData["UploadMessage"] = $"Error in Bulk Upload Handler: {ex.Message}";
                return RedirectToPage();
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostConfirmBulkUploadAsync()
        {
            TempData["UploadMessage"] = null;
            var sessionData = HttpContext.Session.GetString("BulkUploadPreview");

            if (string.IsNullOrEmpty(sessionData))
            {
                TempData["UploadMessage"] = "No records to upload.";
                return RedirectToPage();
            }

            var previewList = JsonSerializer.Deserialize<List<CSVPublisher>>(sessionData);

            if (previewList == null || !previewList.Any())
            {
                TempData["UploadMessage"] = "No records to upload.";
                return RedirectToPage();
            }

            // Get user's Congregation ID
            var user = HttpContext.User;
            string userCongregationId = user.Claims.FirstOrDefault(c => c.Type == "Congregation")?.Value ?? "0";

            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await conn.OpenAsync();

                using (SqlCommand cmd = new SqlCommand("BulkUploadPublishers", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@UserCongregation", userCongregationId);

                    // Convert list to DataTable
                    var dt = new DataTable();
                    dt.Columns.Add("Name", typeof(string));
                    dt.Columns.Add("ServiceGroup", typeof(int));
                    dt.Columns.Add("Privilege", typeof(string));
                    dt.Columns.Add("PhoneNumber", typeof(string));

                    foreach (var record in previewList)
                    {
                        dt.Rows.Add(record.Name, record.ServiceGroup, record.Privilege,
                                    string.IsNullOrWhiteSpace(record.PhoneNumber) ? DBNull.Value : record.PhoneNumber);
                    }

                    var param = cmd.Parameters.AddWithValue("@Publishers", dt);
                    param.SqlDbType = SqlDbType.Structured;
                    param.TypeName = "dbo.PublisherTableType";

                    try
                    {
                        // ✅ Capture inserted row count correctly
                        var insertedRows = await cmd.ExecuteScalarAsync();
                        int insertedCount = insertedRows != null ? Convert.ToInt32(insertedRows) : 0;

                        TempData["UploadMessage"] = $"Bulk upload successful! {insertedCount} record(s) inserted (duplicates removed automatically).";
                    }
                    catch (Exception ex)
                    {
                        TempData["UploadMessage"] = $"Database Error: {ex.Message}";
                        return RedirectToPage();
                    }
                }
            }

            HttpContext.Session.Remove("BulkUploadPreview");
            return RedirectToPage();
        }

        private string SanitizePhoneNumber(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return null;

            // Keep only digits and '+'
            string sanitized = new string(phone.Where(c => char.IsDigit(c) || c == '+').ToArray());

            // Ensure it starts with '+'
            if (!sanitized.StartsWith("+") && sanitized.Length > 0)
            {
                sanitized = "+" + sanitized;
            }

            return sanitized;
        }
    }
}