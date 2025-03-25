using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Khronos4.Data;

public class BasePageModel : PageModel
{
    protected readonly AppDbContext _context;

    public string UserFullName { get; private set; } = "Unknown User";
    public string CongregationName { get; private set; } = "Unassigned";
    public string UserEmail { get; private set; } = "Unknown";
    public int CongregationID { get; private set; } = 0;

    public BasePageModel(AppDbContext context)
    {
        _context = context;
    }

    public override void OnPageHandlerExecuting(Microsoft.AspNetCore.Mvc.Filters.PageHandlerExecutingContext context)
    {
        base.OnPageHandlerExecuting(context);
        LoadUserDetails(); // Ensures user details are loaded before the page renders
    }

    public void LoadUserDetails()
    {
        var user = HttpContext.User;

        if (user.Identity != null && user.Identity.IsAuthenticated)
        {
            UserEmail = user.Identity.Name ?? "Unknown";
            UserFullName = user.Claims.FirstOrDefault(c => c.Type == "FullName")?.Value ?? "Unknown User";
            string congregationIdStr = user.Claims.FirstOrDefault(c => c.Type == "Congregation")?.Value ?? "0";
            CongregationID = int.TryParse(congregationIdStr, out int congId) ? congId : 0;
            CongregationName = GetCongregationName(CongregationID.ToString());
        }
    }

    private string GetCongregationName(string congregationId)
    {
        try
        {
            if (int.TryParse(congregationId, out int congId))
            {
                var congregation = _context.Congregations
                    .Where(c => c.CongID == congId)
                    .Select(c => c.Name)
                    .FirstOrDefault();

                return congregation ?? "Unassigned";
            }
            return "Unassigned";
        }
        catch (Exception ex)
        {
            TempData["DebugError"] = $"Error in GetCongregationName: {ex.Message}";
            return "Unassigned";
        }
    }
}
