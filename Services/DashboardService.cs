using Microsoft.EntityFrameworkCore;
using PrinterServiceAPI.Data;
using PrinterServiceAPI.DTOs;

namespace PrinterServiceAPI.Services;

public interface IDashboardService
{
    Task<ApiResponse<DashboardStatsResponse>> GetStatsAsync();
    Task<ApiResponse<TechDashboardStatsResponse>> GetTechnicianStatsAsync(string techCode);
}

public class DashboardService(AppDbContext db, ILogger<DashboardService> logger) : IDashboardService
{
    public async Task<ApiResponse<DashboardStatsResponse>> GetStatsAsync()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var weekAgo = today.AddDays(-6);

        // Each section below runs in isolation via SafeRunAsync. If one query
        // throws, the others still return data, and the failure is logged +
        // reported by name instead of the whole endpoint dying with a generic
        // 500 "An internal server error occurred."
        var errors = new List<string>();

        // Totals
        var totalVisits = await SafeRunAsync("TotalVisits", errors,
            () => db.SiteVisits.CountAsync());

        var totalTechnicians = await SafeRunAsync("TotalTechnicians", errors,
            () => db.Users.CountAsync(u => u.RoleId == 2 && u.IsActive));

        var todayVisits = await SafeRunAsync("TodayVisits", errors,
            () => db.SiteVisits.CountAsync(v => v.VisitDate == today));

        // Top categories
        // NOTE: EF Core can't translate "new CategoryStat(g.Key, g.Count())" used
        // directly inside GroupBy().Select() — positional record constructors break
        // the SQL translator there. Project to an anonymous type first (translates
        // fine to GROUP BY / COUNT(*)), then map to the DTO after materializing.
        var topCategories = await SafeRunAsync("TopCategories", errors, async () =>
        {
            var raw = await db.SiteVisits
                .GroupBy(v => v.Category.CategoryName)
                .Select(g => new { CategoryName = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToArrayAsync();

            return raw.Select(x => new CategoryStat(x.CategoryName, x.Count)).ToArray();
        }) ?? [];

        // Top technicians
        var topTechnicians = await SafeRunAsync("TopTechnicians", errors, async () =>
        {
            var raw = await db.SiteVisits
                .GroupBy(v => new { v.TechnicianCode, v.TechnicianName })
                .Select(g => new { g.Key.TechnicianCode, g.Key.TechnicianName, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .ToArrayAsync();

            return raw.Select(x => new TopTechnicianDto(x.TechnicianCode, x.TechnicianName, x.Count)).ToArray();
        }) ?? [];

        // Weekly trend (last 7 days)
        var weeklyTrend = await SafeRunAsync("WeeklyTrend", errors, async () =>
        {
            var weeklyVisits = await db.SiteVisits
                .Include(v => v.Category)
                .Where(v => v.VisitDate >= weekAgo && v.VisitDate <= today)
                .ToListAsync();

            return Enumerable.Range(0, 7)
                .Select(i =>
                {
                    var date = weekAgo.AddDays(i);
                    var daily = weeklyVisits.Where(v => v.VisitDate == date).ToList();
                    var byCategory = daily
                        .GroupBy(v => v.Category?.CategoryName ?? "Unknown")
                        .Select(g => new CategoryStat(g.Key, g.Count()))
                        .ToArray();

                    return new DailyVisitStat(date.ToString("yyyy-MM-dd"), daily.Count, byCategory);
                })
                .ToArray();
        }) ?? [];

        var stats = new DashboardStatsResponse(
            totalVisits, totalTechnicians, todayVisits, topCategories, topTechnicians, weeklyTrend);

        if (errors.Count > 0)
        {
            var detail = string.Join(" | ", errors);
            logger.LogError("GetStatsAsync: {Count} section(s) failed -> {Detail}", errors.Count, detail);

            // Still 200 OK with whatever data we managed to gather, but Success=false
            // and Message names the exact failing query — check this before digging
            // through console logs.
            return new ApiResponse<DashboardStatsResponse>(false, $"Dashboard stats partially failed: {detail}", stats);
        }

        return new ApiResponse<DashboardStatsResponse>(true, "Success.", stats);
    }

    public async Task<ApiResponse<TechDashboardStatsResponse>> GetTechnicianStatsAsync(string techCode)
    {
        if (string.IsNullOrWhiteSpace(techCode))
        {
            logger.LogWarning("GetTechnicianStatsAsync called with no technician identifier on the token.");
            return new ApiResponse<TechDashboardStatsResponse>(false, "Technician identifier missing from token.", null);
        }

        try
        {
            // The "techCode" value coming from the controller might be the numeric
            // UserId (if the NameIdentifier claim was used) or the literal
            // TechnicianCode (if the "techCode" fallback claim was used).
            // Handle both instead of assuming int.Parse always succeeds.
            var resolvedTechCode = techCode;

            if (int.TryParse(techCode, out var userId))
            {
                var user = await db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
                if (user is null)
                {
                    logger.LogWarning("GetTechnicianStatsAsync: no user found for UserId {UserId}", userId);
                    return new ApiResponse<TechDashboardStatsResponse>(false, $"No technician found for id '{techCode}'.", null);
                }
                resolvedTechCode = user.TechnicianCode;
            }

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var weekAgo = today.AddDays(-6);

            var allTimeVisits = await db.SiteVisits
                .CountAsync(v => v.TechnicianCode == resolvedTechCode);

            var currentWeekVisits = await db.SiteVisits
                .CountAsync(v => v.TechnicianCode == resolvedTechCode
                              && v.VisitDate >= weekAgo
                              && v.VisitDate <= today);

            var todayVisits = await db.SiteVisits
                .CountAsync(v => v.TechnicianCode == resolvedTechCode
                              && v.VisitDate == today);

            return new ApiResponse<TechDashboardStatsResponse>(true, "Success.", new TechDashboardStatsResponse(
                allTimeVisits, currentWeekVisits, todayVisits));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GetTechnicianStatsAsync failed for techCode '{TechCode}'", techCode);
            return new ApiResponse<TechDashboardStatsResponse>(false, $"Failed to load technician stats: {ex.Message}", null);
        }
    }

    // Runs a single query/step in isolation: logs the full exception and records
    // the failure by name instead of letting one bad query take down the whole
    // dashboard response.
    private async Task<T> SafeRunAsync<T>(string step, List<string> errors, Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "DashboardService step '{Step}' failed", step);
            errors.Add($"{step}: {ex.GetType().Name} - {ex.Message}");
            return default!;
        }
    }
}