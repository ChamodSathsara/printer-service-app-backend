using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PrinterServiceAPI.Models;

// ─────────────────────────────────────────────
// Role
// ─────────────────────────────────────────────
public class Role
{
    [Key]
    public int RoleId { get; set; }

    [Required, MaxLength(50)]
    public string RoleName { get; set; } = string.Empty;

    public ICollection<User> Users { get; set; } = [];
}

// ─────────────────────────────────────────────
// User
// ─────────────────────────────────────────────
public class User
{
    [Key]
    public int UserId { get; set; }

    [Required, MaxLength(20)]
    public string TechnicianCode { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? Email { get; set; }

    [Required, MaxLength(256)]
    public string PasswordHash { get; set; } = string.Empty;

    public int RoleId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey(nameof(RoleId))]
    public Role Role { get; set; } = null!;

    public ICollection<SiteVisit> SiteVisits { get; set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
}

// ─────────────────────────────────────────────
// Machine
// ─────────────────────────────────────────────
public class Machine
{
    [Key]
    public int MachineId { get; set; }

    [Required, MaxLength(50)]
    public string MachineRefNumber { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ModelName { get; set; }

    [MaxLength(100)]
    public string? SerialNumber { get; set; }

    [MaxLength(150)]
    public string? CustomerName { get; set; }

    [MaxLength(30)]
    public string? CustomerPhone { get; set; }

    [MaxLength(150)]
    public string? CustomerEmail { get; set; }

    [MaxLength(500)]
    public string? CustomerAddress { get; set; }

    public DateOnly? InstalledDate { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

// ─────────────────────────────────────────────
// SolutionCategory
// ─────────────────────────────────────────────
public class SolutionCategory
{
    [Key]
    public int CategoryId { get; set; }

    [Required, MaxLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<SiteVisit> SiteVisits { get; set; } = [];
}

// ─────────────────────────────────────────────
// SiteVisit
// ─────────────────────────────────────────────
public class SiteVisit
{
    [Key]
    public int VisitId { get; set; }

    public int TechnicianId { get; set; }

    [Required, MaxLength(20)]
    public string TechnicianCode { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string TechnicianName { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string MachineRefNumber { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    [MaxLength(2000)]
    public string? Note { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? MeterReadingValue { get; set; }

    [Column(TypeName = "decimal(10,7)")]
    public decimal? Latitude { get; set; }

    [Column(TypeName = "decimal(10,7)")]
    public decimal? Longitude { get; set; }

    [MaxLength(500)]
    public string? LocationAddress { get; set; }

    public DateOnly VisitDate { get; set; }
    public TimeOnly VisitTime { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey(nameof(TechnicianId))]
    public User Technician { get; set; } = null!;

    [ForeignKey(nameof(CategoryId))]
    public SolutionCategory Category { get; set; } = null!;
}

// ─────────────────────────────────────────────
// RefreshToken
// ─────────────────────────────────────────────
public class RefreshToken
{
    [Key]
    public int TokenId { get; set; }

    public int UserId { get; set; }

    [Required, MaxLength(512)]
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRevoked { get; set; } = false;

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}

// ─────────────────────────────────────────────
// PasswordResetToken
// ─────────────────────────────────────────────
public class PasswordResetToken
{
    [Key]
    public int ResetId { get; set; }

    public int UserId { get; set; }

    [Required, MaxLength(256)]
    public string Token { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}
