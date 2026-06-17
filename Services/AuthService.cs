using Microsoft.EntityFrameworkCore;
using PrinterServiceAPI.Data;
using PrinterServiceAPI.DTOs;
using PrinterServiceAPI.Helpers;
using PrinterServiceAPI.Models;

namespace PrinterServiceAPI.Services;

public interface IAuthService
{
    Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request);
    Task<ApiResponse<LoginResponse>> RefreshAsync(RefreshTokenRequest request);
    Task<ApiResponse<string>>        LogoutAsync(int userId, string refreshToken);
    Task<ApiResponse<string>>        ChangePasswordAsync(int userId, ChangePasswordRequest request);
    Task<ApiResponse<string>>        ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<ApiResponse<string>>        ResetPasswordAsync(ResetPasswordRequest request);
}

public class AuthService(AppDbContext db, IJwtHelper jwt) : IAuthService
{
    // ── Login ────────────────────────────────────────────────────
    public async Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request)
    {
        try
        {
            var user = await db.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.TechnicianCode == request.TechnicianCode && u.IsActive);

            if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return Fail<LoginResponse>("Invalid technician code or password.");

            var accessToken = jwt.GenerateAccessToken(user);
            var refreshToken = jwt.GenerateRefreshToken();

            db.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.UserId,
                Token = refreshToken,
                ExpiresAt = jwt.RefreshTokenExpiry
            });

            await db.SaveChangesAsync();

            return Ok(new LoginResponse(
                accessToken,
                refreshToken,
                user.TechnicianCode,
                user.FullName,
                user.Role.RoleName,
                jwt.AccessTokenExpiry
            ), "Login successful.");
        }
        catch (Exception ex)
        {
            // TEMPORARY - remove after fix
            return Fail<LoginResponse>(
                $"ERROR: {ex.Message} | INNER: {ex.InnerException?.Message} | SOURCE: {ex.Source}");
        }
    }

    // ── Refresh ──────────────────────────────────────────────────
    public async Task<ApiResponse<LoginResponse>> RefreshAsync(RefreshTokenRequest request)
    {
        var stored = await db.RefreshTokens
            .Include(t => t.User).ThenInclude(u => u.Role)
            .FirstOrDefaultAsync(t => t.Token == request.RefreshToken && !t.IsRevoked);

        if (stored is null || stored.ExpiresAt < DateTime.UtcNow)
            return Fail<LoginResponse>("Invalid or expired refresh token.");

        if (!stored.User.IsActive)
            return Fail<LoginResponse>("Account is inactive.");

        // Rotate token
        stored.IsRevoked = true;
        var newRefresh = jwt.GenerateRefreshToken();

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId    = stored.UserId,
            Token     = newRefresh,
            ExpiresAt = jwt.RefreshTokenExpiry
        });

        await db.SaveChangesAsync();

        var accessToken = jwt.GenerateAccessToken(stored.User);

        return Ok(new LoginResponse(
            accessToken,
            newRefresh,
            stored.User.TechnicianCode,
            stored.User.FullName,
            stored.User.Role.RoleName,
            jwt.AccessTokenExpiry
        ), "Token refreshed.");
    }

    // ── Logout ───────────────────────────────────────────────────
    public async Task<ApiResponse<string>> LogoutAsync(int userId, string refreshToken)
    {
        var token = await db.RefreshTokens
            .FirstOrDefaultAsync(t => t.UserId == userId && t.Token == refreshToken);

        if (token is not null)
        {
            token.IsRevoked = true;
            await db.SaveChangesAsync();
        }

        return Ok("Logged out.", "Logged out successfully.");
    }

    // ── Change Password ──────────────────────────────────────────
    public async Task<ApiResponse<string>> ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        if (request.NewPassword != request.ConfirmNewPassword)
            return Fail<string>("New passwords do not match.");

        var user = await db.Users.FindAsync(userId);
        if (user is null) return Fail<string>("User not found.");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            return Fail<string>("Current password is incorrect.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAt    = DateTime.UtcNow;

        // Revoke all refresh tokens
        var tokens = await db.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked).ToListAsync();
        tokens.ForEach(t => t.IsRevoked = true);

        await db.SaveChangesAsync();
        return Ok("Password changed.", "Password changed successfully.");
    }

    // ── Forgot Password (generates reset token) ─────────────────
    public async Task<ApiResponse<string>> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.TechnicianCode == request.TechnicianCode && u.IsActive);

        // Always return success to prevent enumeration
        if (user is null)
            return Ok("If the account exists, a reset token has been generated.",
                      "Reset token generated.");

        var resetToken = Convert.ToHexString(Guid.NewGuid().ToByteArray());

        db.PasswordResetTokens.Add(new PasswordResetToken
        {
            UserId    = user.UserId,
            Token     = resetToken,
            ExpiresAt = DateTime.UtcNow.AddHours(2)
        });

        await db.SaveChangesAsync();

        // In production: send email. Here we return the token directly.
        return Ok(resetToken, "Reset token generated. Share this with the user securely.");
    }

    // ── Reset Password ───────────────────────────────────────────
    public async Task<ApiResponse<string>> ResetPasswordAsync(ResetPasswordRequest request)
    {
        if (request.NewPassword != request.ConfirmNewPassword)
            return Fail<string>("Passwords do not match.");

        var resetEntry = await db.PasswordResetTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == request.Token && !r.IsUsed);

        if (resetEntry is null || resetEntry.ExpiresAt < DateTime.UtcNow)
            return Fail<string>("Invalid or expired reset token.");

        resetEntry.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        resetEntry.User.UpdatedAt    = DateTime.UtcNow;
        resetEntry.IsUsed            = true;

        await db.SaveChangesAsync();
        return Ok("Password reset successfully.", "Done.");
    }

    // ── Helpers ──────────────────────────────────────────────────
    private static ApiResponse<T> Ok<T>(T data, string message = "Success") =>
        new(true, message, data);

    private static ApiResponse<T> Fail<T>(string message) =>
        new(false, message, default);
}
