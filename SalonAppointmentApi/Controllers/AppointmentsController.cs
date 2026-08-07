using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalonAppointmentApi.Data;
using SalonAppointmentApi.DTOs;
using SalonAppointmentApi.Models;
using System.Data;

namespace SalonAppointmentApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController : ControllerBase
{
    private readonly AppDbContext _context;

    public AppointmentsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> CreateAppointment(CreateAppointmentRequest request)
    {
        await using var transaction = await _context.Database
            .BeginTransactionAsync(IsolationLevel.ReadCommitted);

        var business = await _context.Businesses
            .FirstOrDefaultAsync(x => x.Id == request.BusinessId);

        if (business == null)
            return NotFound("İşletme bulunamadı.");

        var employee = await _context.Employees
            .FirstOrDefaultAsync(x =>
                x.Id == request.EmployeeId &&
                x.BusinessId == request.BusinessId);

        if (employee == null)
            return NotFound("Çalışan bulunamadı veya bu işletmeye ait değil.");

        var service = await _context.Services
            .FirstOrDefaultAsync(x =>
                x.Id == request.ServiceId &&
                x.BusinessId == request.BusinessId);

        if (service == null)
            return NotFound("Hizmet bulunamadı veya bu işletmeye ait değil.");

        var canEmployeeDoService = await _context.EmployeeServices
            .AnyAsync(x =>
                x.EmployeeId == request.EmployeeId &&
                x.ServiceId == request.ServiceId);

        if (!canEmployeeDoService)
            return BadRequest("Bu çalışan bu hizmeti yapamıyor.");

        if (request.CustomerUserId.HasValue)
        {
            var customerExists = await _context.AppUsers
                .AnyAsync(x =>
                    x.Id == request.CustomerUserId.Value &&
                    x.Role == "Customer");

            if (!customerExists)
                return BadRequest("Müşteri kullanıcısı bulunamadı.");
        }

        var totalDuration = service.DurationMinutes + service.BufferMinutes;

        var startTime = request.StartTime;
        var endTime = startTime.AddMinutes(totalDuration);
        var now = DateTime.Now;

        if (startTime <= now)
        {
            return BadRequest(
                "Geçmiş tarih veya saat için randevu oluşturulamaz."
            );
        }
        var businessOpenTime = startTime.Date.Add(business.OpenTime);
        var businessCloseTime = startTime.Date.Add(business.CloseTime);

        if (startTime < businessOpenTime || endTime > businessCloseTime)
            return BadRequest("Seçilen saat işletmenin çalışma saatleri dışında.");

        bool hasConflict = await _context.Appointments.AnyAsync(a =>
            a.BusinessId == request.BusinessId &&
            a.EmployeeId == request.EmployeeId &&
            (a.Status == "Confirmed" || a.Status == "PendingPayment") &&
            a.StartTime < endTime &&
            startTime < a.EndTime
        );

        if (hasConflict)
            return Conflict("Bu saat dolu. Lütfen başka bir saat seçin.");

        var totalPrice = service.Price;
        var depositAmount = Math.Round(totalPrice * 0.10m, 2);
        var remainingAmount = totalPrice - depositAmount;

        var appointment = new Appointment
        {
            BusinessId = request.BusinessId,
            EmployeeId = request.EmployeeId,
            ServiceId = request.ServiceId,
            CustomerUserId = request.CustomerUserId,
            CustomerName = request.CustomerName,
            CustomerPhone = request.CustomerPhone,
            StartTime = startTime,
            EndTime = endTime,
            TotalPrice = totalPrice,
            DepositAmount = depositAmount,
            RemainingAmount = remainingAmount,
            Status = "Confirmed",
            PaymentStatus = "DepositPaid",
            PaidAt = DateTime.Now
        };

        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        await transaction.CommitAsync();

        return Ok(appointment);
    }

    [HttpGet("business/{businessId}")]
    public async Task<IActionResult> GetBusinessAppointments(int businessId, DateTime date)
    {
        var dayStart = date.Date;
        var dayEnd = dayStart.AddDays(1);

        var appointments = await _context.Appointments
            .Where(x =>
                x.BusinessId == businessId &&
                x.StartTime >= dayStart &&
                x.StartTime < dayEnd)
            .OrderBy(x => x.StartTime)
            .ToListAsync();

        return Ok(appointments);
    }

    [HttpGet("customer/{customerUserId}")]
    public async Task<IActionResult> GetCustomerAppointments(int customerUserId)
    {
        var appointments = await _context.Appointments
            .Where(x => x.CustomerUserId == customerUserId)
            .OrderByDescending(x => x.StartTime)
            .ToListAsync();

        return Ok(appointments);
    }

    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> CancelAppointment(int id, string? reason = null)
    {
        var appointment = await _context.Appointments.FindAsync(id);

        if (appointment == null)
            return NotFound("Randevu bulunamadı.");

        if (appointment.Status == "Completed")
            return BadRequest("Tamamlanmış randevu iptal edilemez.");

        if (appointment.Status == "NoShow")
            return BadRequest("Gelmedi olarak işaretlenen randevu iptal edilemez.");

        if (appointment.Status == "CancelledRefunded" || appointment.Status == "CancelledLate")
            return BadRequest("Bu randevu zaten iptal edilmiş.");

        var now = DateTime.Now;

        if (now >= appointment.StartTime)
            return BadRequest("Randevu saati geçtiği için müşteri iptal edemez. İşletme sahibi gelmedi olarak işaretlemelidir.");

        var freeCancelDeadline = appointment.StartTime.AddHours(-24);

        appointment.CancelledAt = now;
        appointment.CancellationReason = reason;

        if (now <= freeCancelDeadline)
        {
            appointment.Status = "CancelledRefunded";
            appointment.PaymentStatus = "DepositRefunded";

            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Randevu zamanında iptal edildi. Kapora iade edilecek.",
                appointment
            });
        }


        appointment.Status = "CancelledLate";
        appointment.PaymentStatus = "DepositKept";

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Randevu geç iptal edildi. Kapora iade edilmeyecek.",
            appointment
        });
    }

    [HttpPut("{id}/complete")]
    public async Task<IActionResult> CompleteAppointment(int id)
    {
        var appointment = await _context.Appointments.FindAsync(id);

        if (appointment == null)
            return NotFound("Randevu bulunamadı.");

        if (appointment.Status != "Confirmed")
            return BadRequest("Sadece onaylı randevular tamamlanabilir.");

        appointment.Status = "Completed";
        appointment.PaymentStatus = "Completed";
        appointment.CompletedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Randevu tamamlandı. Kalan ödeme işletmede alınmalıdır.",
            remainingAmount = appointment.RemainingAmount,
            appointment
        });
    }

    [HttpPut("{id}/no-show")]
    public async Task<IActionResult> MarkNoShow(int id)
    {
        var appointment = await _context.Appointments.FindAsync(id);

        if (appointment == null)
            return NotFound("Randevu bulunamadı.");

        if (appointment.Status != "Confirmed")
            return BadRequest("Sadece onaylı randevular gelmedi olarak işaretlenebilir.");

        appointment.Status = "NoShow";
        appointment.PaymentStatus = "DepositKept";

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Müşteri gelmedi olarak işaretlendi. Kapora iade edilmeyecek.",
            appointment
        });
    }
    [HttpGet("business/{businessId}/all")]
    public async Task<IActionResult> GetAllBusinessAppointments(int businessId)
    {
        var appointments = await _context.Appointments
            .Where(x => x.BusinessId == businessId)
            .OrderByDescending(x => x.StartTime)
            .ToListAsync();

        return Ok(appointments);
    }

}