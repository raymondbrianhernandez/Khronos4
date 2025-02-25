using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Khronos4.Data;
using Khronos4.Models;
using System;
using System.Data;
using System.Threading.Tasks;

namespace Khronos4.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UpdatePlacementController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UpdatePlacementController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Placement placementData)
        {
            if (placementData == null || string.IsNullOrWhiteSpace(placementData.Owner))
            {
                return BadRequest(new { success = false, message = "Missing required fields." });
            }

            try
            {
                var parameters = new[]
                {
                    new SqlParameter("@Owner", SqlDbType.NVarChar) { Value = placementData.Owner },
                    new SqlParameter("@Date", SqlDbType.Date) { Value = placementData.Date },
                    new SqlParameter("@Hours", SqlDbType.Decimal) { Value = placementData.Hours },
                    new SqlParameter("@Placements", SqlDbType.Int) { Value = placementData.Placements },
                    new SqlParameter("@RVs", SqlDbType.Int) { Value = placementData.RVs },
                    new SqlParameter("@BS", SqlDbType.Int) { Value = placementData.BS },
                    new SqlParameter("@LDC", SqlDbType.Decimal) { Value = placementData.LDC },
                    new SqlParameter("@Notes", SqlDbType.NVarChar) { Value = string.IsNullOrWhiteSpace(placementData.Notes) ? (object)DBNull.Value : placementData.Notes }
                };

                int rowsAffected = await _context.Database.ExecuteSqlRawAsync(
                    "EXEC [dbo].[UpdatePlacement] @Owner, @Date, @Hours, @Placements, @RVs, @BS, @LDC, @Notes",
                    parameters);

                return Ok(new { success = true, rowsUpdated = rowsAffected });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }
}
