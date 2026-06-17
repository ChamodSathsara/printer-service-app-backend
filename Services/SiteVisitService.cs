using Microsoft.EntityFrameworkCore;
using PrinterServiceAPI.Data;
using PrinterServiceAPI.DTOs;
using PrinterServiceAPI.Models;

namespace PrinterServiceAPI.Services;

public interface ISiteVisitService
{
    Task<ApiResponse<SiteVisitResponse>> CreateAsync(int technicianId, CreateSiteVisitRequest request);
    Task<ApiResponse<PagedResult<SiteVisitResponse>>> GetMyVisitsAsync(int technicianId, SiteVisitListRequest filter);
    Task<ApiResponse<PagedResult<SiteVisitResponse>>> GetAllVisitsAsync(SiteVisitListRequest filter);
    Task<ApiResponse<SiteVisitResponse>>              GetByIdAsync(int visitId);
}

public class SiteVisitService(AppDbContext db) : ISiteVisitService
{
    // ── Create Site Visit ────────────────────────────────────────
    public async Task<ApiResponse<SiteVisitResponse>> CreateAsync(int technicianId, CreateSiteVisitRequest req)
    {
        var user = await db.Users.FindAsync(technicianId);
        if (user is null) return Fail<SiteVisitResponse>("Technician not found.");

        var category = await db.SolutionCategories
            .FirstOrDefaultAsync(c => c.CategoryId == req.CategoryId && c.IsActive);
        if (category is null) return Fail<SiteVisitResponse>("Invalid solution category.");

        var now   = DateTime.UtcNow;
        var visit = new SiteVisit
        {
            TechnicianId    = technicianId,
            TechnicianCode  = user.TechnicianCode,
            TechnicianName  = user.FullName,
            MachineRefNumber = req.MachineRefNumber.Trim(),
            CategoryId      = req.CategoryId,
            Note            = req.Note?.Trim(),
            MeterReadingValue = req.MeterReadingValue,
            Latitude        = req.Latitude,
            Longitude       = req.Longitude,
            LocationAddress = req.LocationAddress?.Trim(),
            VisitDate       = DateOnly.FromDateTime(now),
            VisitTime       = TimeOnly.FromDateTime(now),
            CreatedAt       = now
        };

        db.SiteVisits.Add(visit);
        await db.SaveChangesAsync();

        return Ok(MapToDto(visit, category.CategoryName), "Site visit recorded.");
    }

    // ── My Visits (Technician) ───────────────────────────────────
    public async Task<ApiResponse<PagedResult<SiteVisitResponse>>> GetMyVisitsAsync(
        int technicianId, SiteVisitListRequest filter)
    {
        var query = db.SiteVisits
            .Include(v => v.Category)
            .Where(v => v.TechnicianId == technicianId);

        query = ApplyFilters(query, filter);

        return Ok(await ToPagedAsync(query, filter), "Success.");
    }

    // ── All Visits (Manager) ─────────────────────────────────────
    public async Task<ApiResponse<PagedResult<SiteVisitResponse>>> GetAllVisitsAsync(SiteVisitListRequest filter)
    {
        var query = db.SiteVisits.Include(v => v.Category).AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.TechnicianCode))
            query = query.Where(v => v.TechnicianCode == filter.TechnicianCode);

        query = ApplyFilters(query, filter);

        return Ok(await ToPagedAsync(query, filter), "Success.");
    }

    // ── Get By Id ────────────────────────────────────────────────
    public async Task<ApiResponse<SiteVisitResponse>> GetByIdAsync(int visitId)
    {
        var visit = await db.SiteVisits
            .Include(v => v.Category)
            .FirstOrDefaultAsync(v => v.VisitId == visitId);

        if (visit is null) return Fail<SiteVisitResponse>("Visit not found.");

        return Ok(MapToDto(visit, visit.Category.CategoryName), "Success.");
    }

    // ── Private Helpers ──────────────────────────────────────────
    private static IQueryable<SiteVisit> ApplyFilters(IQueryable<SiteVisit> q, SiteVisitListRequest f)
    {
        if (f.FromDate.HasValue)
            q = q.Where(v => v.VisitDate >= f.FromDate.Value);

        if (f.ToDate.HasValue)
            q = q.Where(v => v.VisitDate <= f.ToDate.Value);

        if (f.CategoryId.HasValue)
            q = q.Where(v => v.CategoryId == f.CategoryId.Value);

        if (!string.IsNullOrWhiteSpace(f.Search))
        {
            var s = f.Search.Trim().ToLower();
            q = q.Where(v =>
                v.MachineRefNumber.ToLower().Contains(s) ||
                v.TechnicianName.ToLower().Contains(s)   ||
                (v.Note != null && v.Note.ToLower().Contains(s)));
        }

        return q.OrderByDescending(v => v.VisitDate).ThenByDescending(v => v.VisitTime);
    }

    private static async Task<PagedResult<SiteVisitResponse>> ToPagedAsync(
        IQueryable<SiteVisit> query, SiteVisitListRequest filter)
    {
        var total = await query.CountAsync();
        var page  = Math.Max(1, filter.Page);
        var size  = Math.Clamp(filter.PageSize, 1, 100);

        var items = await query
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync();

        return new PagedResult<SiteVisitResponse>(
            items.Select(v => MapToDto(v, v.Category.CategoryName)),
            total,
            page,
            size,
            (int)Math.Ceiling(total / (double)size)
        );
    }

    private static SiteVisitResponse MapToDto(SiteVisit v, string categoryName) =>
        new(
            v.VisitId,
            v.TechnicianCode,
            v.TechnicianName,
            v.MachineRefNumber,
            v.CategoryId,
            categoryName,
            v.Note,
            v.MeterReadingValue,
            v.Latitude,
            v.Longitude,
            v.LocationAddress,
            v.VisitDate.ToString("yyyy-MM-dd"),
            v.VisitTime.ToString("HH:mm:ss"),
            v.CreatedAt
        );

    private static ApiResponse<T> Ok<T>(T data, string message) => new(true, message, data);
    private static ApiResponse<T> Fail<T>(string message) => new(false, message, default);
}
