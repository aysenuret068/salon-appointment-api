using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalonAppointmentApi.Data;

namespace SalonAppointmentApi.Controllers;

[ApiController, Route("api/admin"), Authorize(Roles = "Admin")]
public sealed class AdminResourcesController(AppDbContext db) : ControllerBase
{
    static int PageNumber(int value) => Math.Max(1, value);
    static int PageSize(int value) => Math.Clamp(value, 1, 100);
    static object Result<T>(List<T> items, int total, int page, int size) => new { items, total, page, pageSize = size, totalPages = (int)Math.Ceiling(total / (double)size) };
    async Task<IActionResult> Page<T>(IQueryable<T> query, int page, int size) { page = PageNumber(page); size = PageSize(size); var total = await query.CountAsync(); var items = await query.Skip((page - 1) * size).Take(size).ToListAsync(); return Ok(Result(items, total, page, size)); }

    [HttpGet("users")]
    public async Task<IActionResult> Users(int page=1,int pageSize=25,string? search=null){var q=db.AppUsers.AsNoTracking().Where(x=>!x.IsDeleted);if(!string.IsNullOrWhiteSpace(search))q=q.Where(x=>x.FullName.Contains(search)||x.Email.Contains(search)||x.Phone.Contains(search));return await Page(q.OrderByDescending(x=>x.CreatedAt).Select(x=>new{x.Id,x.FullName,x.Email,x.Phone,x.Role,x.IsActive,x.CreatedAt,x.UpdatedAt}),page,pageSize);}
    [HttpGet("users/{id:int}")]
    public async Task<IActionResult> UserDetails(int id){var x=await db.AppUsers.AsNoTracking().Where(x=>x.Id==id&&!x.IsDeleted).Select(x=>new{x.Id,x.FullName,x.Email,x.Phone,x.Role,x.IsActive,x.CreatedAt,x.UpdatedAt,businesses=db.Businesses.Count(b=>b.OwnerUserId==x.Id&&!b.IsDeleted),appointments=db.Appointments.Count(a=>a.CustomerUserId==x.Id),reviews=db.Reviews.Count(r=>r.CustomerId==x.Id)}).SingleOrDefaultAsync();return x==null?NotFound(new{message="Kullanıcı bulunamadı."}):Ok(x);}
    [HttpGet("businesses")]
    public async Task<IActionResult> Businesses(int page=1,int pageSize=25,string? search=null){var q=db.Businesses.AsNoTracking().Where(x=>!x.IsDeleted);if(!string.IsNullOrWhiteSpace(search))q=q.Where(x=>x.Name.Contains(search));return await Page(q.OrderBy(x=>x.Name).Select(x=>new{x.Id,x.Name,x.Address,x.Phone,x.OpenTime,x.CloseTime,x.OwnerUserId,owner=x.OwnerUser==null?null:x.OwnerUser.FullName,x.IsActive,x.CreatedAt,employees=x.Employees.Count(e=>!e.IsDeleted),services=x.Services.Count(s=>!s.IsDeleted)}),page,pageSize);}
    [HttpGet("employees")]
    public async Task<IActionResult> Employees(int page=1,int pageSize=25,string? search=null){var q=db.Employees.AsNoTracking().Where(x=>!x.IsDeleted);if(!string.IsNullOrWhiteSpace(search))q=q.Where(x=>x.FullName.Contains(search));return await Page(q.OrderBy(x=>x.FullName).Select(x=>new{x.Id,x.FullName,x.BusinessId,business=x.Business!.Name,services=x.EmployeeServices.Count,x.IsActive}),page,pageSize);}
    [HttpGet("services")]
    public async Task<IActionResult> Services(int page=1,int pageSize=25,string? search=null){var q=db.Services.AsNoTracking().Where(x=>!x.IsDeleted);if(!string.IsNullOrWhiteSpace(search))q=q.Where(x=>x.Name.Contains(search));return await Page(q.OrderBy(x=>x.Name).Select(x=>new{x.Id,x.Name,x.BusinessId,business=x.Business!.Name,x.DurationMinutes,x.BufferMinutes,x.Price,x.IsActive}),page,pageSize);}
    [HttpGet("employee-services")]
    public async Task<IActionResult> EmployeeServices(int page=1,int pageSize=25,int? businessId=null,int? employeeId=null){var q=db.EmployeeServices.AsNoTracking().Where(x=>!x.Employee!.IsDeleted&&!x.Service!.IsDeleted);if(businessId.HasValue)q=q.Where(x=>x.Employee!.BusinessId==businessId);if(employeeId.HasValue)q=q.Where(x=>x.EmployeeId==employeeId);return await Page(q.OrderBy(x=>x.Id).Select(x=>new{x.Id,business=x.Employee!.Business!.Name,x.EmployeeId,employee=x.Employee.FullName,x.ServiceId,service=x.Service!.Name}),page,pageSize);}
    [HttpGet("appointments")]
    public async Task<IActionResult> Appointments(int page=1,int pageSize=25,string? search=null){var q=db.Appointments.AsNoTracking();if(!string.IsNullOrWhiteSpace(search))q=q.Where(x=>x.CustomerName.Contains(search)||x.CustomerPhone.Contains(search)||x.Status.Contains(search));return await Page(q.OrderByDescending(x=>x.StartTime).Select(x=>new{x.Id,x.CustomerName,x.CustomerPhone,x.BusinessId,x.EmployeeId,x.ServiceId,x.StartTime,x.EndTime,x.Status,x.TotalPrice,x.DepositAmount,x.RemainingAmount,x.PaymentStatus}),page,pageSize);}
    [HttpGet("reviews")]
    public async Task<IActionResult> Reviews(int page=1,int pageSize=25){return await Page(db.Reviews.AsNoTracking().OrderByDescending(x=>x.CreatedAt).Select(x=>new{x.Id,x.AppointmentId,x.BusinessId,business=x.Business!.Name,x.CustomerId,customer=x.Customer!.FullName,x.EmployeeId,x.Rating,x.Comment,x.IsPublished,x.CreatedAt}),page,pageSize);}
    [HttpGet("content")] public Task<IActionResult> Content(int page=1,int pageSize=25)=>Page(db.ContentItems.AsNoTracking().OrderBy(x=>x.Group).ThenBy(x=>x.Key),page,pageSize);
    [HttpGet("announcements")] public Task<IActionResult> Announcements(int page=1,int pageSize=25)=>Page(db.Announcements.AsNoTracking().OrderByDescending(x=>x.CreatedAt),page,pageSize);
    [HttpGet("faq")] public Task<IActionResult> Faq(int page=1,int pageSize=25)=>Page(db.FaqItems.AsNoTracking().OrderBy(x=>x.SortOrder),page,pageSize);
    [HttpGet("media")] public Task<IActionResult> Media(int page=1,int pageSize=25)=>Page(db.MediaAssets.AsNoTracking().OrderByDescending(x=>x.CreatedAt),page,pageSize);
    [HttpGet("settings")] public Task<IActionResult> Settings(int page=1,int pageSize=25)=>Page(db.AppSettings.AsNoTracking().Where(x=>!x.Key.Contains("Secret")&&!x.Key.Contains("Password")&&!x.Key.Contains("Token")&&!x.Key.Contains("Key")).OrderBy(x=>x.Group).ThenBy(x=>x.Key),page,pageSize);
    [HttpGet("audit-logs")] public Task<IActionResult> AuditLogs(int page=1,int pageSize=25)=>Page(db.AdminAuditLogs.AsNoTracking().OrderByDescending(x=>x.CreatedAt),page,pageSize);

    [HttpGet("lookups/business-owners")]
    public async Task<IActionResult> BusinessOwners() => Ok(await db.AppUsers.AsNoTracking()
        .Where(x => x.Role == "BusinessOwner" && x.IsActive && !x.IsDeleted)
        .OrderBy(x => x.FullName).Select(x => new { x.Id, x.FullName, x.Email }).ToListAsync());

    [HttpGet("lookups/businesses")]
    public async Task<IActionResult> BusinessLookup() => Ok(await db.Businesses.AsNoTracking()
        .Where(x => x.IsActive && !x.IsDeleted).OrderBy(x => x.Name)
        .Select(x => new { x.Id, x.Name }).ToListAsync());

    [HttpGet("reports")] public async Task<IActionResult> Reports(DateTime? from=null,DateTime? to=null){var start=(from??DateTime.UtcNow.AddDays(-30)).Date;var end=(to??DateTime.UtcNow).Date.AddDays(1);var q=db.Appointments.AsNoTracking().Where(x=>x.StartTime>=start&&x.StartTime<end);return Ok(new{from=start,to=end.AddDays(-1),appointments=await q.CountAsync(),completed=await q.CountAsync(x=>x.Status=="Completed"),cancelled=await q.CountAsync(x=>x.Status=="CancelledRefunded"||x.Status=="CancelledLate"),revenue=await q.Where(x=>x.Status=="Completed").SumAsync(x=>(decimal?)x.TotalPrice)??0});}
    [HttpGet("system-status")] public async Task<IActionResult> Status(){var start=DateTime.UtcNow;var connected=await db.Database.CanConnectAsync();return Ok(new{status=connected?"Healthy":"Unhealthy",database=connected?"Connected":"Disconnected",checkedAtUtc=DateTime.UtcNow,responseMilliseconds=(DateTime.UtcNow-start).TotalMilliseconds});}
}