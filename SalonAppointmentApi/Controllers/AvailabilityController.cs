using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalonAppointmentApi.Data;

namespace SalonAppointmentApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AvailabilityController : ControllerBase
{
    private readonly AppDbContext _context;

    public AvailabilityController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAvailableSlots(
        int businessId,
        int employeeId,
        int serviceId,
        DateTime date)
    {
        var business = await _context.Businesses
            .FirstOrDefaultAsync(x => x.Id == businessId);

        if (business == null)
            return NotFound("İşletme bulunamadı.");

        var employee = await _context.Employees
            .FirstOrDefaultAsync(x => x.Id == employeeId && x.BusinessId == businessId);

        if (employee == null)
            return NotFound("Çalışan bulunamadı veya bu işletmeye ait değil.");

        var service = await _context.Services
            .FirstOrDefaultAsync(x => x.Id == serviceId && x.BusinessId == businessId);

        if (service == null)
            return NotFound("Hizmet bulunamadı veya bu işletmeye ait değil.");

        var canEmployeeDoService = await _context.EmployeeServices
            .AnyAsync(x => x.EmployeeId == employeeId && x.ServiceId == serviceId);

        if (!canEmployeeDoService)
            return BadRequest("Bu çalışan seçilen hizmeti yapamıyor.");

        int totalDuration = service.DurationMinutes + service.BufferMinutes;

        var dayStart = date.Date.Add(business.OpenTime);
        var dayEnd = date.Date.Add(business.CloseTime);

        var appointments = await _context.Appointments
            .Where(x =>
                x.BusinessId == businessId &&
                x.EmployeeId == employeeId &&
                (x.Status == "Confirmed" || x.Status == "PendingPayment") &&
                x.StartTime >= dayStart &&
                x.StartTime < dayEnd)
            .ToListAsync();

        var availableSlots = new List<object>();

        var slotInterval = TimeSpan.FromMinutes(15);
        var current = dayStart;

        while (current.AddMinutes(totalDuration) <= dayEnd)
        {
            var possibleStart = current;
            var possibleEnd = current.AddMinutes(totalDuration);

            bool hasConflict = appointments.Any(a =>
                a.StartTime < possibleEnd &&
                possibleStart < a.EndTime
            );

            if (!hasConflict)
            {
                availableSlots.Add(new
                {
                    startTime = possibleStart,
                    endTime = possibleEnd,
                    totalMinutes = totalDuration
                });
            }

            current = current.Add(slotInterval);
        }

        return Ok(availableSlots);
    }
}