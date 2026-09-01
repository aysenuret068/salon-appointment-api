using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalonAppointmentApi.Data;

namespace SalonAppointmentApi.Controllers;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class AdminDashboardController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var now = DateTime.Now;
        var today = now.Date;
        var tomorrow = today.AddDays(1);
        var users = db.AppUsers.AsNoTracking().Where(x => !x.IsDeleted);
        var appointments = db.Appointments.AsNoTracking();
        var reviews = db.Reviews.AsNoTracking();

        var result = new
        {
            totalUsers = await users.CountAsync(cancellationToken),
            customers = await users.CountAsync(x => x.Role == "Customer", cancellationToken),
            businessOwners = await users.CountAsync(x => x.Role == "BusinessOwner", cancellationToken),
            admins = await users.CountAsync(x => x.Role == "Admin" || x.Role == "SuperAdmin", cancellationToken),
            totalBusinesses = await db.Businesses.AsNoTracking().CountAsync(cancellationToken),
            activeBusinesses = await db.Businesses.AsNoTracking().CountAsync(cancellationToken),
            totalEmployees = await db.Employees.AsNoTracking().CountAsync(cancellationToken),
            totalServices = await db.Services.AsNoTracking().CountAsync(cancellationToken),
            todayAppointments = await appointments.CountAsync(x => x.StartTime >= today && x.StartTime < tomorrow, cancellationToken),
            upcomingAppointments = await appointments.CountAsync(x => x.StartTime >= now && x.Status == "Confirmed", cancellationToken),
            completedAppointments = await appointments.CountAsync(x => x.Status == "Completed", cancellationToken),
            cancelledAppointments = await appointments.CountAsync(x => x.Status == "Cancelled", cancellationToken),
            noShowAppointments = await appointments.CountAsync(x => x.Status == "NoShow", cancellationToken),
            totalReviews = await reviews.CountAsync(cancellationToken),
            averageRating = await reviews.Select(x => (double?)x.Rating).AverageAsync(cancellationToken) ?? 0,
            last7DaysAppointments = await appointments.Where(x => x.StartTime >= today.AddDays(-6)).GroupBy(x => x.StartTime.Date).Select(x => new { date = x.Key, count = x.Count() }).ToListAsync(cancellationToken),
            appointmentStatusDistribution = await appointments.GroupBy(x => x.Status).Select(x => new { status = x.Key, count = x.Count() }).ToListAsync(cancellationToken),
            newUsers = await users.OrderByDescending(x => x.CreatedAt).Take(8).Select(x => new { x.Id, x.FullName, x.Role, x.CreatedAt }).ToListAsync(cancellationToken)
        };
        return Ok(result);
    }
}
