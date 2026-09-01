using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalonAppointmentApi.Data;

namespace SalonAppointmentApi.Controllers;

[ApiController, Route("api/admin"), Authorize(Roles="Admin,SuperAdmin")]
public sealed class AdminResourcesController(AppDbContext db) : ControllerBase
{
    static int P(int x)=>Math.Max(1,x); static int S(int x)=>Math.Clamp(x,1,100);
    static object Result<T>(List<T> items,int total,int page,int size)=>new{items,total,page,pageSize=size,totalPages=(int)Math.Ceiling(total/(double)size)};
    async Task<IActionResult> Page<T>(IQueryable<T> query,int page,int size){page=P(page);size=S(size);var total=await query.CountAsync();var items=await query.Skip((page-1)*size).Take(size).ToListAsync();return Ok(Result(items,total,page,size));}

    [HttpGet("users")] public async Task<IActionResult> Users(int page=1,int pageSize=25,string? search=null){var q=db.AppUsers.AsNoTracking().Where(x=>!x.IsDeleted);if(!string.IsNullOrWhiteSpace(search))q=q.Where(x=>x.FullName.Contains(search)||x.Email.Contains(search));return await Page(q.OrderByDescending(x=>x.CreatedAt).Select(x=>new{x.Id,x.FullName,x.Email,x.Phone,x.Role,x.IsActive,x.CreatedAt}),page,pageSize);}
    [HttpGet("businesses")] public async Task<IActionResult> Businesses(int page=1,int pageSize=25,string? search=null){var q=db.Businesses.AsNoTracking();if(!string.IsNullOrWhiteSpace(search))q=q.Where(x=>x.Name.Contains(search));return await Page(q.OrderBy(x=>x.Name).Select(x=>new{x.Id,x.Name,x.Address,x.Phone,x.OwnerUserId,owner=x.OwnerUser==null?null:x.OwnerUser.FullName,employees=x.Employees.Count,services=x.Services.Count}),page,pageSize);}
    [HttpGet("employees")] public async Task<IActionResult> Employees(int page=1,int pageSize=25){return await Page(db.Employees.AsNoTracking().OrderBy(x=>x.FullName).Select(x=>new{x.Id,x.FullName,x.BusinessId,business=x.Business!.Name,services=x.EmployeeServices.Count}),page,pageSize);}
    [HttpGet("services")] public async Task<IActionResult> Services(int page=1,int pageSize=25){return await Page(db.Services.AsNoTracking().OrderBy(x=>x.Name).Select(x=>new{x.Id,x.Name,x.BusinessId,business=x.Business!.Name,x.DurationMinutes,x.BufferMinutes,x.Price}),page,pageSize);}
    [HttpGet("employee-services")] public async Task<IActionResult> EmployeeServices(int page=1,int pageSize=25){return await Page(db.EmployeeServices.AsNoTracking().OrderBy(x=>x.Id).Select(x=>new{x.Id,x.EmployeeId,employee=x.Employee!.FullName,x.ServiceId,service=x.Service!.Name}),page,pageSize);}
    [HttpGet("appointments")] public async Task<IActionResult> Appointments(int page=1,int pageSize=25){return await Page(db.Appointments.AsNoTracking().OrderByDescending(x=>x.StartTime).Select(x=>new{x.Id,x.CustomerName,x.CustomerPhone,x.BusinessId,x.EmployeeId,x.ServiceId,x.StartTime,x.EndTime,x.Status,x.TotalPrice,x.PaymentStatus}),page,pageSize);}
    [HttpGet("reviews")] public async Task<IActionResult> Reviews(int page=1,int pageSize=25){return await Page(db.Reviews.AsNoTracking().OrderByDescending(x=>x.CreatedAt).Select(x=>new{x.Id,x.AppointmentId,x.BusinessId,x.CustomerId,x.EmployeeId,x.Rating,x.Comment,x.CreatedAt}),page,pageSize);}
    [HttpGet("content")] public Task<IActionResult> Content(int page=1,int pageSize=25)=>Page(db.ContentItems.AsNoTracking().OrderBy(x=>x.Group).ThenBy(x=>x.Key),page,pageSize);
    [HttpGet("announcements")] public Task<IActionResult> Announcements(int page=1,int pageSize=25)=>Page(db.Announcements.AsNoTracking().OrderByDescending(x=>x.CreatedAt),page,pageSize);
    [HttpGet("faq")] public Task<IActionResult> Faq(int page=1,int pageSize=25)=>Page(db.FaqItems.AsNoTracking().OrderBy(x=>x.SortOrder),page,pageSize);
    [HttpGet("media")] public Task<IActionResult> Media(int page=1,int pageSize=25)=>Page(db.MediaAssets.AsNoTracking().OrderByDescending(x=>x.CreatedAt),page,pageSize);
    [HttpGet("settings")] public Task<IActionResult> Settings(int page=1,int pageSize=25)=>Page(db.AppSettings.AsNoTracking().OrderBy(x=>x.Group).ThenBy(x=>x.Key),page,pageSize);
    [HttpGet("audit-logs")] public Task<IActionResult> AuditLogs(int page=1,int pageSize=25)=>Page(db.AdminAuditLogs.AsNoTracking().OrderByDescending(x=>x.CreatedAt),page,pageSize);
    [HttpGet("reports")] public async Task<IActionResult> Reports(DateTime? from=null,DateTime? to=null){var start=(from??DateTime.UtcNow.AddDays(-30)).Date;var end=(to??DateTime.UtcNow).Date.AddDays(1);var q=db.Appointments.AsNoTracking().Where(x=>x.StartTime>=start&&x.StartTime<end);return Ok(new{from=start,to=end.AddDays(-1),appointments=await q.CountAsync(),completed=await q.CountAsync(x=>x.Status=="Completed"),cancelled=await q.CountAsync(x=>x.Status=="Cancelled"),revenue=await q.Where(x=>x.Status=="Completed").SumAsync(x=>(decimal?)x.TotalPrice)??0});}
    [HttpGet("system-status")] public async Task<IActionResult> Status(){var start=DateTime.UtcNow;var connected=await db.Database.CanConnectAsync();return Ok(new{status=connected?"Healthy":"Unhealthy",database=connected?"Connected":"Disconnected",checkedAtUtc=DateTime.UtcNow,responseMilliseconds=(DateTime.UtcNow-start).TotalMilliseconds});}
}