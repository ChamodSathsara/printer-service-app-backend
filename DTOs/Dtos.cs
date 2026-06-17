namespace PrinterServiceAPI.DTOs;

// ════════════════════════════════════════════
// AUTH
// ════════════════════════════════════════════

public record LoginRequest(
    string TechnicianCode,
    string Password
);

public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    string TechnicianCode,
    string FullName,
    string Role,
    DateTime ExpiresAt
);

public record RefreshTokenRequest(string RefreshToken);

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword,
    string ConfirmNewPassword
);

public record ResetPasswordRequest(
    string Token,
    string NewPassword,
    string ConfirmNewPassword
);

public record ForgotPasswordRequest(string TechnicianCode);

// ════════════════════════════════════════════
// SITE VISIT
// ════════════════════════════════════════════

public record CreateSiteVisitRequest(
    string  MachineRefNumber,
    int     CategoryId,
    string? Note,
    decimal? MeterReadingValue,
    decimal? Latitude,
    decimal? Longitude,
    string? LocationAddress
);

public record SiteVisitResponse(
    int     VisitId,
    string  TechnicianCode,
    string  TechnicianName,
    string  MachineRefNumber,
    int     CategoryId,
    string  CategoryName,
    string? Note,
    decimal? MeterReadingValue,
    decimal? Latitude,
    decimal? Longitude,
    string? LocationAddress,
    string  VisitDate,      // "yyyy-MM-dd"
    string  VisitTime,      // "HH:mm:ss"
    DateTime CreatedAt
);

public record SiteVisitListRequest(
    DateOnly? FromDate,
    DateOnly? ToDate,
    string?   Search,
    string?   TechnicianCode,
    int?      CategoryId,
    int       Page    = 1,
    int       PageSize = 20
);

public record PagedResult<T>(
    IEnumerable<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);

// ════════════════════════════════════════════
// TECHNICIAN / USER
// ════════════════════════════════════════════

public record TechnicianDto(
    int    UserId,
    string TechnicianCode,
    string FullName,
    string? Email,
    bool   IsActive,
    int    VisitCount
);

public record CreateTechnicianRequest(
    string TechnicianCode,
    string FullName,
    string? Email,
    string Password
);

public record UpdateTechnicianRequest(
    string FullName,
    string? Email,
    bool IsActive
);

public record TechnicianProfileResponse(
    int     UserId,
    string  TechnicianCode,
    string  FullName,
    string? Email,
    bool    IsActive,
    int     TotalVisits,
    SiteVisitResponse[] RecentVisits,
    CategoryStat[]  CategoryBreakdown
);

public record CategoryStat(string CategoryName, int Count);

// ════════════════════════════════════════════
// MACHINES
// ════════════════════════════════════════════

public record MachineDto(
    int     MachineId,
    string  MachineRefNumber,
    string? ModelName,
    string? SerialNumber,
    string? CustomerName,
    string? CustomerPhone,
    string? CustomerEmail,
    string? CustomerAddress,
    string? InstalledDate
);

public record CreateMachineRequest(
    string  MachineRefNumber,
    string? ModelName,
    string? SerialNumber,
    string? CustomerName,
    string? CustomerPhone,
    string? CustomerEmail,
    string? CustomerAddress,
    DateOnly? InstalledDate
);

// ════════════════════════════════════════════
// DASHBOARD
// ════════════════════════════════════════════

public record DashboardStatsResponse(
    int TotalVisits,
    int TotalTechnicians,
    int TodayVisits,
    CategoryStat[] TopCategories,
    TopTechnicianDto[] TopTechnicians,
    DailyVisitStat[] WeeklyTrend
);

public record TechDashboardStatsResponse(
    int AllTimeVisits,
    int CurrentWeekVisits,
    int TodayVisits
);

public record TopTechnicianDto(
    string TechnicianCode,
    string FullName,
    int    VisitCount
);

public record DailyVisitStat(
    string Date,
    int    TotalCount,
    CategoryStat[] ByCategory
);

// ════════════════════════════════════════════
// SOLUTION CATEGORIES
// ════════════════════════════════════════════

public record SolutionCategoryDto(
    int    CategoryId,
    string CategoryName,
    int    SortOrder
);

// ════════════════════════════════════════════
// COMMON
// ════════════════════════════════════════════

public record ApiResponse<T>(
    bool   Success,
    string Message,
    T?     Data = default
);

public record ApiError(string Message, Dictionary<string, string[]>? Errors = null);
