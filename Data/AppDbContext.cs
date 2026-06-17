using Microsoft.EntityFrameworkCore;
using PrinterServiceAPI.Models;

namespace PrinterServiceAPI.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Role>                 Roles                { get; set; }
    public DbSet<User>                 Users                { get; set; }
    public DbSet<Machine>              Machines             { get; set; }
    public DbSet<SolutionCategory>     SolutionCategories   { get; set; }
    public DbSet<SiteVisit>            SiteVisits           { get; set; }
    public DbSet<RefreshToken>         RefreshTokens        { get; set; }
    public DbSet<PasswordResetToken>   PasswordResetTokens  { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Roles ──────────────────────────────────────────────
        modelBuilder.Entity<Role>(e =>
        {
            e.ToTable("Roles");
            e.HasIndex(r => r.RoleName).IsUnique();
        });

        // ── Users ──────────────────────────────────────────────
        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.HasIndex(u => u.TechnicianCode).IsUnique();

            e.HasOne(u => u.Role)
             .WithMany(r => r.Users)
             .HasForeignKey(u => u.RoleId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Machines ────────────────────────────────────────────
        modelBuilder.Entity<Machine>(e =>
        {
            e.ToTable("Machines");
            e.HasIndex(m => m.MachineRefNumber).IsUnique();
        });

        // ── SolutionCategories ──────────────────────────────────
        modelBuilder.Entity<SolutionCategory>(e =>
        {
            e.ToTable("SolutionCategories");
            e.HasIndex(c => c.CategoryName).IsUnique();
        });

        // ── SiteVisits ──────────────────────────────────────────
        modelBuilder.Entity<SiteVisit>(e =>
        {
            e.ToTable("SiteVisits");

            e.HasOne(v => v.Technician)
             .WithMany(u => u.SiteVisits)
             .HasForeignKey(v => v.TechnicianId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(v => v.Category)
             .WithMany(c => c.SiteVisits)
             .HasForeignKey(v => v.CategoryId)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasIndex(v => v.TechnicianId);
            e.HasIndex(v => v.VisitDate);
            e.HasIndex(v => v.MachineRefNumber);
        });

        // ── RefreshTokens ───────────────────────────────────────
        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.ToTable("RefreshTokens");

            e.HasOne(t => t.User)
             .WithMany(u => u.RefreshTokens)
             .HasForeignKey(t => t.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(t => t.Token);
        });

        // ── PasswordResetTokens ─────────────────────────────────
        modelBuilder.Entity<PasswordResetToken>(e =>
        {
            e.ToTable("PasswordResetTokens");

            e.HasOne(t => t.User)
             .WithMany()
             .HasForeignKey(t => t.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Seed Data ───────────────────────────────────────────
        modelBuilder.Entity<Role>().HasData(
            new Role { RoleId = 1, RoleName = "Manager" },
            new Role { RoleId = 2, RoleName = "Technician" }
        );

        modelBuilder.Entity<SolutionCategory>().HasData(
            new SolutionCategory { CategoryId = 1,  CategoryName = "Toner Inquiry Visit",            SortOrder = 1  },
            new SolutionCategory { CategoryId = 2,  CategoryName = "New Machine Visit",              SortOrder = 2  },
            new SolutionCategory { CategoryId = 3,  CategoryName = "Toner Delivery",                 SortOrder = 3  },
            new SolutionCategory { CategoryId = 4,  CategoryName = "Tender Submission Visit",        SortOrder = 4  },
            new SolutionCategory { CategoryId = 5,  CategoryName = "Tender Reading Visit",           SortOrder = 5  },
            new SolutionCategory { CategoryId = 6,  CategoryName = "Debt Follow-up",                 SortOrder = 6  },
            new SolutionCategory { CategoryId = 7,  CategoryName = "Cash Collection",                SortOrder = 7  },
            new SolutionCategory { CategoryId = 8,  CategoryName = "Cheque Collection",              SortOrder = 8  },
            new SolutionCategory { CategoryId = 9,  CategoryName = "Fake Toner Visit",               SortOrder = 9  },
            new SolutionCategory { CategoryId = 10, CategoryName = "Tender Follow-ups",              SortOrder = 10 },
            new SolutionCategory { CategoryId = 11, CategoryName = "Toner Routine Sales Follow-ups", SortOrder = 11 }
        );
    }
}
