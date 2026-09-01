using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalonAppointmentApi.Auth;
using SalonAppointmentApi.Data;
using SalonAppointmentApi.DTOs;

namespace SalonAppointmentApi.Controllers;

[ApiController]
[Route("api/admin/auth")]
public sealed class AdminAuthController(AppDbContext context, ITokenService tokenService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var email = request.Email?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(request.Password))
            return Unauthorized("Email veya şifre hatalı.");

        var user = await context.AppUsers.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Email == email && !x.IsDeleted);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized("Email veya şifre hatalı.");

        if (!user.IsActive)
            return StatusCode(StatusCodes.Status403Forbidden, "Kullanıcı hesabı devre dışı.");

        if (user.Role is not ("Admin" or "SuperAdmin"))
            return StatusCode(StatusCodes.Status403Forbidden,
                "Bu hesabın yönetim paneline erişim yetkisi bulunmuyor.");

        var token = tokenService.CreateAccessToken(user);
        return Ok(new LoginResponse
        {
            AccessToken = token.Token,
            ExpiresAtUtc = token.ExpiresAtUtc,
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            Role = user.Role
        });
    }
}
