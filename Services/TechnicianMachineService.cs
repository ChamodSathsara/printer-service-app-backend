using Microsoft.EntityFrameworkCore;
using PrinterServiceAPI.Data;
using PrinterServiceAPI.DTOs;
using PrinterServiceAPI.Models;

namespace PrinterServiceAPI.Services;

// ════════════════════════════════════════════
// Technician Service
// ════════════════════════════════════════════
public interface ITechnicianService
{
    Task<ApiResponse<IEnumerable<TechnicianDto>>>    GetAllAsync();
    Task<ApiResponse<TechnicianProfileResponse>>     GetProfileAsync(string techCode);
    Task<ApiResponse<TechnicianDto>>                 CreateAsync(CreateTechnicianRequest request);
    Task<ApiResponse<TechnicianDto>>                 UpdateAsync(string techCode, UpdateTechnicianRequest request);
    Task<ApiResponse<string>>                        DeleteAsync(string techCode);
}

public class TechnicianService(AppDbContext db) : ITechnicianService
{
    public async Task<ApiResponse<IEnumerable<TechnicianDto>>> GetAllAsync()
    {
        var technicians = await db.Users
            .Where(u => u.RoleId == 2)
            .Select(u => new
            {
                u.UserId, u.TechnicianCode, u.FullName, u.Email, u.IsActive,
                VisitCount = u.SiteVisits.Count
            })
            .OrderBy(u => u.FullName)
            .ToListAsync();

        var result = technicians.Select(u =>
            new TechnicianDto(u.UserId, u.TechnicianCode, u.FullName, u.Email, u.IsActive, u.VisitCount));

        return new(true, "Success.", result);
    }

    public async Task<ApiResponse<TechnicianProfileResponse>> GetProfileAsync(string techCode)
    {
        var user = await db.Users
            .Include(u => u.SiteVisits).ThenInclude(v => v.Category)
            .FirstOrDefaultAsync(u => u.TechnicianCode == techCode && u.RoleId == 2);

        if (user is null) return new(false, "Technician not found.", null);

        var recentVisits = user.SiteVisits
            .OrderByDescending(v => v.VisitDate).ThenByDescending(v => v.VisitTime)
            .Take(10)
            .Select(v => new SiteVisitResponse(
                v.VisitId, v.TechnicianCode, v.TechnicianName, v.MachineRefNumber,
                v.CategoryId, v.Category.CategoryName, v.Note, v.MeterReadingValue,
                v.Latitude, v.Longitude, v.LocationAddress,
                v.VisitDate.ToString("yyyy-MM-dd"), v.VisitTime.ToString("HH:mm:ss"), v.CreatedAt))
            .ToArray();

        var breakdown = user.SiteVisits
            .GroupBy(v => v.Category.CategoryName)
            .Select(g => new CategoryStat(g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .ToArray();

        var profile = new TechnicianProfileResponse(
            user.UserId, user.TechnicianCode, user.FullName, user.Email,
            user.IsActive, user.SiteVisits.Count, recentVisits, breakdown);

        return new(true, "Success.", profile);
    }

    public async Task<ApiResponse<TechnicianDto>> CreateAsync(CreateTechnicianRequest request)
    {
        if (await db.Users.AnyAsync(u => u.TechnicianCode == request.TechnicianCode))
            return new(false, "Technician code already exists.", null);

        var user = new User
        {
            TechnicianCode = request.TechnicianCode.Trim(),
            FullName       = request.FullName.Trim(),
            Email          = request.Email?.Trim(),
            PasswordHash   = BCrypt.Net.BCrypt.HashPassword(request.Password),
            RoleId         = 2
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return new(true, "Technician created.", new TechnicianDto(
            user.UserId, user.TechnicianCode, user.FullName, user.Email, user.IsActive, 0));
    }

    public async Task<ApiResponse<TechnicianDto>> UpdateAsync(string techCode, UpdateTechnicianRequest request)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.TechnicianCode == techCode && u.RoleId == 2);

        if (user is null) return new(false, "Technician not found.", null);

        user.FullName  = request.FullName.Trim();
        user.Email     = request.Email?.Trim();
        user.IsActive  = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        var visitCount = await db.SiteVisits.CountAsync(v => v.TechnicianId == user.UserId);

        return new(true, "Updated.", new TechnicianDto(
            user.UserId, user.TechnicianCode, user.FullName, user.Email, user.IsActive, visitCount));
    }

    public async Task<ApiResponse<string>> DeleteAsync(string techCode)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.TechnicianCode == techCode && u.RoleId == 2);

        if (user is null) return new(false, "Technician not found.", null);

        user.IsActive  = false;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return new(true, "Technician deactivated.", "Done.");
    }
}

// ════════════════════════════════════════════
// Machine Service
// ════════════════════════════════════════════
public interface IMachineService
{
    Task<ApiResponse<MachineDto?>>           GetByRefAsync(string refNumber);
    Task<ApiResponse<IEnumerable<MachineDto>>> SearchAsync(string query);
    Task<ApiResponse<MachineDto>>            CreateAsync(CreateMachineRequest request);
}

public class MachineService(AppDbContext db) : IMachineService
{
    public async Task<ApiResponse<MachineDto?>> GetByRefAsync(string refNumber)
    {
        var m = await db.Machines
            .FirstOrDefaultAsync(x => x.MachineRefNumber == refNumber && x.IsActive);

        if (m is null) return new(false, "Machine not found.", null);
        return new(true, "Success.", Map(m));
    }

    public async Task<ApiResponse<IEnumerable<MachineDto>>> SearchAsync(string query)
    {
        var s = query.Trim().ToLower();
        var machines = await db.Machines
            .Where(m => m.IsActive && (
                m.MachineRefNumber.ToLower().Contains(s) ||
                (m.CustomerName  != null && m.CustomerName.ToLower().Contains(s)) ||
                (m.SerialNumber  != null && m.SerialNumber.ToLower().Contains(s))))
            .Take(20)
            .ToListAsync();

        return new(true, "Success.", machines.Select(Map));
    }

    public async Task<ApiResponse<MachineDto>> CreateAsync(CreateMachineRequest request)
    {
        if (await db.Machines.AnyAsync(m => m.MachineRefNumber == request.MachineRefNumber))
            return new(false, "Machine reference number already exists.", null!);

        var machine = new Machine
        {
            MachineRefNumber = request.MachineRefNumber.Trim(),
            ModelName        = request.ModelName?.Trim(),
            SerialNumber     = request.SerialNumber?.Trim(),
            CustomerName     = request.CustomerName?.Trim(),
            CustomerPhone    = request.CustomerPhone?.Trim(),
            CustomerEmail    = request.CustomerEmail?.Trim(),
            CustomerAddress  = request.CustomerAddress?.Trim(),
            InstalledDate    = request.InstalledDate
        };

        db.Machines.Add(machine);
        await db.SaveChangesAsync();

        return new(true, "Machine created.", Map(machine));
    }

    private static MachineDto Map(Machine m) => new(
        m.MachineId,
        m.MachineRefNumber,
        m.ModelName,
        m.SerialNumber,
        m.CustomerName,
        m.CustomerPhone,
        m.CustomerEmail,
        m.CustomerAddress,
        m.InstalledDate?.ToString("yyyy-MM-dd")
    );
}
