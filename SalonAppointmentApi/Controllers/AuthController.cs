using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalonAppointmentApi.Data;
using SalonAppointmentApi.DTOs;
using SalonAppointmentApi.Models;

namespace SalonAppointmentApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;

    public AuthController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        request.Email = request.Email.Trim().ToLower();
        request.Role = request.Role.Trim();

        if (string.IsNullOrWhiteSpace(request.FullName))
            return BadRequest("Ad soyad zorunludur.");

        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest("Email zorunludur.");

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            return BadRequest("Şifre en az 6 karakter olmalıdır.");

        if (request.Role != "Customer" && request.Role != "BusinessOwner")
            return BadRequest("Geçersiz kullanıcı rolü.");

        var emailExists = await _context.AppUsers
            .AnyAsync(x => x.Email == request.Email);

        if (emailExists)
            return BadRequest("Bu email zaten kayıtlı.");

        var user = new AppUser
        {
            FullName = request.FullName,
            Email = request.Email,
            Phone = request.Phone,
            Role = request.Role,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        _context.AppUsers.Add(user);
        await _context.SaveChangesAsync();

        var response = new LoginResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            Role = user.Role
        };

        return Ok(response);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        request.Email = request.Email.Trim().ToLower();

        var user = await _context.AppUsers
            .FirstOrDefaultAsync(x => x.Email == request.Email);

        if (user == null)
            return Unauthorized("Email veya şifre hatalı.");

        var passwordIsValid = BCrypt.Net.BCrypt.Verify(
            request.Password,
            user.PasswordHash
        );

        if (!passwordIsValid)
            return Unauthorized("Email veya şifre hatalı.");

        var response = new LoginResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            Role = user.Role
        };

        return Ok(response);
    }
    public class ResetBusinessOwnerPasswordRequest
    {
        public string BusinessName { get; set; } = "";
        public string NewPassword { get; set; } = "";
    }

    [HttpPut("reset-business-owner-password")]
    public async Task<IActionResult> ResetBusinessOwnerPassword(
        ResetBusinessOwnerPasswordRequest request)
    {
        var business = await _context.Businesses
            .FirstOrDefaultAsync(x => x.Name.ToLower() == request.BusinessName.ToLower());

        if (business == null)
        {
            return NotFound("İşletme bulunamadı.");
        }

        var owner = await _context.AppUsers
            .FirstOrDefaultAsync(x => x.Id == business.OwnerUserId);

        if (owner == null)
        {
            return NotFound("Bu işletmeye bağlı işletme sahibi hesabı bulunamadı.");
        }

        owner.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "İşletme sahibi şifresi başarıyla sıfırlandı.",
            businessName = business.Name,
            ownerUserId = owner.Id,
            email = owner.Email,
            newPassword = request.NewPassword
        });
    }
}