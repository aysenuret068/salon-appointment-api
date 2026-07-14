using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalonAppointmentApi.Data;
using SalonAppointmentApi.DTOs;
using SalonAppointmentApi.Models;

namespace SalonAppointmentApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeeServicesController : ControllerBase
{
    private readonly AppDbContext _context;

    public EmployeeServicesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> AssignServiceToEmployee(AssignServiceToEmployeeRequest request)
    {
        var employeeExists = await _context.Employees
            .AnyAsync(x => x.Id == request.EmployeeId);

        if (!employeeExists)
            return NotFound("Çalışan bulunamadı.");

        var serviceExists = await _context.Services
            .AnyAsync(x => x.Id == request.ServiceId);

        if (!serviceExists)
            return NotFound("Hizmet bulunamadı.");

        var alreadyAssigned = await _context.EmployeeServices
            .AnyAsync(x =>
                x.EmployeeId == request.EmployeeId &&
                x.ServiceId == request.ServiceId);

        if (alreadyAssigned)
            return BadRequest("Bu hizmet zaten bu çalışana atanmış.");

        var employeeService = new EmployeeService
        {
            EmployeeId = request.EmployeeId,
            ServiceId = request.ServiceId
        };

        _context.EmployeeServices.Add(employeeService);
        await _context.SaveChangesAsync();

        return Ok(employeeService);
    }

    [HttpGet("employee/{employeeId}")]
    public async Task<IActionResult> GetEmployeeServices(int employeeId)
    {
        var services = await _context.EmployeeServices
            .Where(x => x.EmployeeId == employeeId)
            .Include(x => x.Service)
            .ToListAsync();

        return Ok(services);
    }
    [HttpGet("business/{businessId}/service/{serviceId}/employees")]
    public async Task<IActionResult> GetEmployeesByBusinessAndService(
    int businessId,
    int serviceId)
    {
        var employees = await _context.EmployeeServices
            .Where(x =>
                x.ServiceId == serviceId &&
                x.Employee != null &&
                x.Employee.BusinessId == businessId)
            .Include(x => x.Employee)
            .Select(x => x.Employee)
            .ToListAsync();

        return Ok(employees);
    }
}