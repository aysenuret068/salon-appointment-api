using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalonAppointmentApi.Data;
using SalonAppointmentApi.DTOs;
using SalonAppointmentApi.Models;

namespace SalonAppointmentApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServicesController : ControllerBase
{
    private readonly AppDbContext _context;

    public ServicesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("business/{businessId}")]
    public async Task<IActionResult> GetServicesByBusiness(int businessId)
    {
        var services = await _context.Services
            .Where(x => x.BusinessId == businessId && !x.IsDeleted && x.IsActive)
            .ToListAsync();

        return Ok(services);
    }

    [HttpPost]
    public async Task<IActionResult> CreateService(CreateServiceRequest request)
    {
        var businessExists = await _context.Businesses
            .AnyAsync(x => x.Id == request.BusinessId);

        if (!businessExists)
            return NotFound("İşletme bulunamadı.");

        var service = new ServiceItem
        {
            BusinessId = request.BusinessId,
            Name = request.Name,
            DurationMinutes = request.DurationMinutes,
            BufferMinutes = request.BufferMinutes,
            Price = request.Price
        };

        _context.Services.Add(service);
        await _context.SaveChangesAsync();

        return Ok(service);
    }
}