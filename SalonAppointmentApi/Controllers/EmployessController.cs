using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalonAppointmentApi.Data;
using SalonAppointmentApi.DTOs;
using SalonAppointmentApi.Models;

namespace SalonAppointmentApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly AppDbContext _context;

    public EmployeesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("business/{businessId}")]
    public async Task<IActionResult> GetEmployeesByBusiness(int businessId)
    {
        var employees = await _context.Employees
            .Where(x => x.BusinessId == businessId && !x.IsDeleted && x.IsActive)
            .ToListAsync();

        return Ok(employees);
    }

    [HttpPost]
    public async Task<IActionResult> CreateEmployee(CreateEmployeeRequest request)
    {
        var businessExists = await _context.Businesses
            .AnyAsync(x => x.Id == request.BusinessId);

        if (!businessExists)
            return NotFound("İşletme bulunamadı.");

        var employee = new Employee
        {
            BusinessId = request.BusinessId,
            FullName = request.FullName
        };

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        return Ok(employee);
    }
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEmployee(int id)
    {
        var employee = await _context.Employees.FindAsync(id);

        if (employee == null)
            return NotFound("Çalışan bulunamadı.");

        var now = DateTime.Now;

        var hasActiveAppointment = await _context.Appointments.AnyAsync(x =>
            x.EmployeeId == id &&
            x.Status == "Confirmed" &&
            x.StartTime >= now);

        if (hasActiveAppointment)
            return BadRequest("Bu çalışanın aktif randevusu olduğu için silinemez.");

        var employeeServices = await _context.EmployeeServices
            .Where(x => x.EmployeeId == id)
            .ToListAsync();

        _context.EmployeeServices.RemoveRange(employeeServices);
        _context.Employees.Remove(employee);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Çalışan başarıyla silindi."
        });
    }
}
