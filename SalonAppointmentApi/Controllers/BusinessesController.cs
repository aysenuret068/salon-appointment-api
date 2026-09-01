using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalonAppointmentApi.Data;
using SalonAppointmentApi.DTOs;
using SalonAppointmentApi.Models;

namespace SalonAppointmentApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BusinessesController : ControllerBase
{
    private readonly AppDbContext _context;

    public BusinessesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetBusinesses()
    {
        var businesses = await _context.Businesses
            .OrderBy(x => x.Name)
            .ToListAsync();

        return Ok(businesses);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetBusinessById(int id)
    {
        var business = await _context.Businesses.FindAsync(id);

        if (business == null)
            return NotFound("İşletme bulunamadı.");

        return Ok(business);
    }

    [HttpGet("owner/{ownerUserId}")]
    public async Task<IActionResult> GetBusinessesByOwner(int ownerUserId)
    {
        var businesses = await _context.Businesses
            .Where(x => x.OwnerUserId == ownerUserId)
            .OrderBy(x => x.Name)
            .ToListAsync();

        return Ok(businesses);
    }

    [HttpPost]
    public async Task<IActionResult> CreateBusiness(CreateBusinessRequest request)
    {
        if (request.OwnerUserId.HasValue)
        {
            var ownerExists = await _context.AppUsers.AnyAsync(x =>
                x.Id == request.OwnerUserId.Value &&
                x.Role == "BusinessOwner");

            if (!ownerExists)
                return BadRequest("İşletme sahibi kullanıcı bulunamadı.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("İşletme adı zorunludur.");

        if (request.OpenTime >= request.CloseTime)
            return BadRequest("Açılış saati kapanış saatinden küçük olmalıdır.");

        var business = new Business
        {
            OwnerUserId = request.OwnerUserId,
            Name = request.Name.Trim(),
            Address = request.Address,
            Phone = request.Phone,
            OpenTime = request.OpenTime,
            CloseTime = request.CloseTime
        };

        _context.Businesses.Add(business);
        await _context.SaveChangesAsync();

        return Ok(business);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBusiness(int id, UpdateBusinessRequest request)
    {
        var business = await _context.Businesses.FindAsync(id);

        if (business == null)
            return NotFound("İşletme bulunamadı.");

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("İşletme adı zorunludur.");

        if (request.OpenTime >= request.CloseTime)
            return BadRequest("Açılış saati kapanış saatinden küçük olmalıdır.");

        business.Name = request.Name.Trim();
        business.Address = request.Address;
        business.Phone = request.Phone;
        business.OpenTime = request.OpenTime;
        business.CloseTime = request.CloseTime;

        await _context.SaveChangesAsync();

        return Ok(business);
    }
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteBusiness(int id)
    {
        var business = await _context.Businesses
            .FirstOrDefaultAsync(x => x.Id == id);

        if (business == null)
        {
            return NotFound("İşletme bulunamadı.");
        }

        var hasActiveAppointments = await _context.Appointments
            .AnyAsync(x =>
                x.BusinessId == id &&
                (
                    x.Status == "Confirmed" ||
                    x.Status == "PendingPayment"
                )
            );

        if (hasActiveAppointments)
        {
            return BadRequest(
                "Bu işletmenin aktif randevuları bulunduğu için işletme silinemez."
            );
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            var appointments = await _context.Appointments
                .Where(x => x.BusinessId == id)
                .ToListAsync();

            var employees = await _context.Employees
                .Where(x => x.BusinessId == id)
                .ToListAsync();

            var employeeIds = employees
                .Select(x => x.Id)
                .ToList();

            var services = await _context.Services
                .Where(x => x.BusinessId == id)
                .ToListAsync();

            var serviceIds = services
                .Select(x => x.Id)
                .ToList();

            var employeeServices = await _context.EmployeeServices
                .Where(x =>
                    employeeIds.Contains(x.EmployeeId) ||
                    serviceIds.Contains(x.ServiceId))
                .ToListAsync();

            var reviews = await _context.Reviews
                .Where(x => x.BusinessId == id)
                .ToListAsync();

            _context.Reviews.RemoveRange(reviews);
            _context.Appointments.RemoveRange(appointments);
            _context.EmployeeServices.RemoveRange(employeeServices);
            _context.Employees.RemoveRange(employees);
            _context.Services.RemoveRange(services);
            _context.Businesses.Remove(business);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new
            {
                message = "İşletme başarıyla silindi."
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            return StatusCode(
                500,
                $"İşletme silinirken hata oluştu: {ex.Message}"
            );
        }
    }
}