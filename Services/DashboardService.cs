using Microsoft.EntityFrameworkCore;
using PrinterServiceAPI.Data;
using PrinterServiceAPI.DTOs;

namespace PrinterServiceAPI.Services;

public interface IDashboardService
{
    Task<ApiResponse<DashboardStatsResponse>> GetStatsAsync();
    Task<ApiResponse<TechDashboardStatsResponse>> GetTechnicianStatsAsync(string techCode);
}



public class DashboardService(AppDbContext db) : IDashboardService
{
    public async Task<ApiResponse<DashboardStatsResponse>> GetStatsAsync()
    {
        var today   = DateOnly.FromDateTime(DateTime.UtcNow);
        var weekAgo = today.AddDays(-6);

        // Totals
        var totalVisits      = await db.SiteVisits.CountAsync();
        var totalTechnicians = await db.Users.CountAsync(u => u.RoleId == 2 && u.IsActive);
        var todayVisits      = await db.SiteVisits.CountAsync(v => v.VisitDate == today);

        // Top categories
        var topCategories = await db.SiteVisits
            .GroupBy(v => v.Category.CategoryName)
            .Select(g => new CategoryStat(g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToArrayAsync();

        // Top technicians
        var topTechnicians = await db.SiteVisits
            .GroupBy(v => new { v.TechnicianCode, v.TechnicianName })
            .Select(g => new TopTechnicianDto(g.Key.TechnicianCode, g.Key.TechnicianName, g.Count()))
            .OrderByDescending(x => x.VisitCount)
            .Take(5)
            .ToArrayAsync();

        // Weekly trend (last 7 days)
        var weeklyVisits = await db.SiteVisits
            .Include(v => v.Category)
            .Where(v => v.VisitDate >= weekAgo && v.VisitDate <= today)
            .ToListAsync();

        var weeklyTrend = Enumerable.Range(0, 7)
            .Select(i =>
            {
                var date  = weekAgo.AddDays(i);
                var daily = weeklyVisits.Where(v => v.VisitDate == date).ToList();

                var byCategory = daily
                    .GroupBy(v => v.Category.CategoryName)
                    .Select(g => new CategoryStat(g.Key, g.Count()))
                    .ToArray();

                return new DailyVisitStat(
                    date.ToString("yyyy-MM-dd"),
                    daily.Count,
                    byCategory
                );
            })
            .ToArray();

        return new ApiResponse<DashboardStatsResponse>(true, "Success.", new DashboardStatsResponse(
            totalVisits,
            totalTechnicians,
            todayVisits,
            topCategories,
            topTechnicians,
            weeklyTrend
        ));
    }


    public async Task<ApiResponse<TechDashboardStatsResponse>> GetTechnicianStatsAsync(string techCode)
    {
        int id = int.Parse(techCode);
        var user = await db.Users.FirstOrDefaultAsync(u => u.UserId == id);
        var newTechCode = user.TechnicianCode;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var weekAgo = today.AddDays(-6);

        var allTimeVisits = await db.SiteVisits
            .CountAsync(v => v.TechnicianCode == newTechCode);

        var currentWeekVisits = await db.SiteVisits
            .CountAsync(v => v.TechnicianCode == newTechCode
                          && v.VisitDate >= weekAgo
                          && v.VisitDate <= today);

        var todayVisits = await db.SiteVisits
            .CountAsync(v => v.TechnicianCode == newTechCode
                          && v.VisitDate == today);

        return new ApiResponse<TechDashboardStatsResponse>(true, "Success.", new TechDashboardStatsResponse(
            allTimeVisits,
            currentWeekVisits,
            todayVisits
        ));
    }


}
