using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrinterServiceAPI.DTOs;
using PrinterServiceAPI.Services;

namespace PrinterServiceAPI.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    // POST /api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await authService.LoginAsync(request);
        return result.Success ? Ok(result) : Unauthorized(result);
    }

    // POST /api/auth/refresh
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        var result = await authService.RefreshAsync(request);
        return result.Success ? Ok(result) : Unauthorized(result);
    }

    // POST /api/auth/logout
    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await authService.LogoutAsync(userId, request.RefreshToken);
        return Ok(result);
    }

    // POST /api/auth/change-password
    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await authService.ChangePasswordAsync(userId, request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // POST /api/auth/forgot-password
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var result = await authService.ForgotPasswordAsync(request);
        return Ok(result);
    }

    // POST /api/auth/reset-password
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var result = await authService.ResetPasswordAsync(request);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    // GET /api/auth/me
    [Authorize]
    [HttpGet("me")]
    public IActionResult Me() => Ok(new
    {
        UserId         = User.FindFirstValue(ClaimTypes.NameIdentifier),
        TechnicianCode = User.FindFirstValue("techCode"),
        FullName       = User.FindFirstValue("fullName"),
        Role           = User.FindFirstValue(ClaimTypes.Role)
    });


    [HttpGet("gen-hash")]
    public IActionResult GenHash() =>
    Ok(BCrypt.Net.BCrypt.HashPassword("Admin@123"));


    [HttpGet("gen-hash/{password}")]
    public IActionResult GenHash(string password) =>
    Ok(new { hash = BCrypt.Net.BCrypt.HashPassword(password) });
}
