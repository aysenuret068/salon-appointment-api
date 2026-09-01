using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalonAppointmentApi.Data;
using SalonAppointmentApi.Models;

namespace SalonAppointmentApi.Controllers;

[ApiController,Route("api/admin"),Authorize(Roles="Admin")]
public sealed class AdminManagementController(AppDbContext db,IWebHostEnvironment env) : ControllerBase
{
    int AdminId=>int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out var id)?id:0;
    async Task Audit(string action,string type,object? id,object? value=null){db.AdminAuditLogs.Add(new AdminAuditLog{AdminUserId=AdminId,Action=action,EntityType=type,EntityId=id?.ToString(),Description=action+" "+type,NewValues=value==null?null:JsonSerializer.Serialize(value),IPAddress=HttpContext.Connection.RemoteIpAddress?.ToString()});await db.SaveChangesAsync();}
    static bool SecretKey(string key)=>new[]{"secret","password","token","key"}.Any(x=>key.Contains(x,StringComparison.OrdinalIgnoreCase));

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser(UserCreate input)
    {
        var email = input.Email?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(input.FullName) || string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(input.Phone) || string.IsNullOrWhiteSpace(input.Password))
            return BadRequest(new { message = "Ad soyad, e-posta, telefon ve şifre zorunludur." });
        if (!System.Net.Mail.MailAddress.TryCreate(email, out _))
            return BadRequest(new { message = "Geçerli bir e-posta adresi girin." });
        if (input.Password.Length < 8)
            return BadRequest(new { message = "Şifre en az 8 karakter olmalıdır." });
        if (input.Role is not ("Customer" or "BusinessOwner" or "Admin"))
            return BadRequest(new { message = "Geçersiz rol." });
        if (await db.AppUsers.AnyAsync(x => x.Email == email && !x.IsDeleted))
            return Conflict(new { message = "Bu e-posta zaten kullanılıyor." });
        var user = new AppUser { FullName = input.FullName.Trim(), Email = email, Phone = input.Phone.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(input.Password), Role = input.Role,
            IsActive = input.IsActive, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.AppUsers.Add(user); await db.SaveChangesAsync();
        await Audit("UserCreated", "User", user.Id, new { user.FullName, user.Email, user.Phone, user.Role, user.IsActive });
        return Ok(new { user.Id, user.FullName, user.Email, user.Phone, user.Role, user.IsActive, user.CreatedAt });
    }

    [HttpPost("businesses")]
    public async Task<IActionResult> CreateBusiness(BusinessCreate input)
    {
        if (string.IsNullOrWhiteSpace(input.Name) || input.CloseTime <= input.OpenTime)
            return BadRequest(new { message = "İşletme adı ve çalışma saatleri geçerli olmalıdır." });
        if (!await db.AppUsers.AnyAsync(x => x.Id == input.OwnerUserId && x.Role == "BusinessOwner" && x.IsActive && !x.IsDeleted))
            return BadRequest(new { message = "Geçerli ve aktif bir işletme sahibi seçin." });
        var business = new Business { Name = input.Name.Trim(), Address = input.Address?.Trim(), Phone = input.Phone?.Trim(),
            OpenTime = input.OpenTime, CloseTime = input.CloseTime, OwnerUserId = input.OwnerUserId,
            IsActive = input.IsActive, CreatedAt = DateTime.UtcNow };
        db.Businesses.Add(business); await db.SaveChangesAsync(); await Audit("BusinessCreated", "Business", business.Id, input);
        return Ok(business);
    }

    [HttpPost("employees")]
    public async Task<IActionResult> CreateEmployee(EmployeeCreate input)
    {
        if (string.IsNullOrWhiteSpace(input.FullName))
            return BadRequest(new { message = "Çalışan adı zorunludur." });
        if (!await db.Businesses.AnyAsync(x => x.Id == input.BusinessId && x.IsActive && !x.IsDeleted))
            return BadRequest(new { message = "Geçerli ve aktif bir işletme seçin." });
        var employee = new Employee { FullName = input.FullName.Trim(), BusinessId = input.BusinessId, IsActive = input.IsActive };
        db.Employees.Add(employee); await db.SaveChangesAsync(); await Audit("EmployeeCreated", "Employee", employee.Id, input);
        return Ok(employee);
    }

    [HttpPost("services")]
    public async Task<IActionResult> CreateService(ServiceCreate input)
    {
        if (string.IsNullOrWhiteSpace(input.Name) || input.Price < 0 || input.DurationMinutes <= 0 || input.BufferMinutes < 0)
            return BadRequest(new { message = "Hizmet adı, fiyat ve süre bilgileri geçerli olmalıdır." });
        if (!await db.Businesses.AnyAsync(x => x.Id == input.BusinessId && x.IsActive && !x.IsDeleted))
            return BadRequest(new { message = "Geçerli ve aktif bir işletme seçin." });
        var service = new ServiceItem { Name = input.Name.Trim(), BusinessId = input.BusinessId, Price = input.Price,
            DurationMinutes = input.DurationMinutes, BufferMinutes = input.BufferMinutes, IsActive = input.IsActive };
        db.Services.Add(service); await db.SaveChangesAsync(); await Audit("ServiceCreated", "Service", service.Id, input);
        return Ok(service);
    }


    [HttpPatch("users/{id:int}")]
    public async Task<IActionResult> UpdateUser(int id,UserUpdate input){var x=await db.AppUsers.FindAsync(id);if(x==null||x.IsDeleted)return NotFound(new{message="Kullanıcı bulunamadı."});if(string.IsNullOrWhiteSpace(input.FullName)||string.IsNullOrWhiteSpace(input.Email))return BadRequest(new{message="Ad soyad ve e-posta zorunludur."});var email=input.Email.Trim().ToLowerInvariant();if(await db.AppUsers.AnyAsync(u=>u.Id!=id&&!u.IsDeleted&&u.Email==email))return Conflict(new{message="Bu e-posta başka bir kullanıcı tarafından kullanılıyor."});var roles=new[]{"Customer","BusinessOwner","Admin"};if(!roles.Contains(input.Role))return BadRequest(new{message="Geçersiz rol."});if(id==AdminId&&(!input.IsActive||input.Role!="Admin"))return Conflict(new{message="Kendi aktif Admin rolünüzü değiştiremezsiniz."});x.FullName=input.FullName.Trim();x.Email=email;x.Phone=input.Phone.Trim();x.Role=input.Role;x.IsActive=input.IsActive;x.UpdatedAt=DateTime.UtcNow;await db.SaveChangesAsync();await Audit("UserUpdated","User",id,new{x.FullName,x.Email,x.Phone,x.Role,x.IsActive});return Ok(new{x.Id,x.FullName,x.Email,x.Phone,x.Role,x.IsActive});}
    [HttpPatch("users/{id:int}/status")]
    public async Task<IActionResult> UserStatus(int id,StatusUpdate input){var x=await db.AppUsers.FindAsync(id);if(x==null||x.IsDeleted)return NotFound(new{message="Kullanıcı bulunamadı."});if(id==AdminId&&!input.IsActive)return Conflict(new{message="Kendi hesabınızı pasifleştiremezsiniz."});x.IsActive=input.IsActive;x.UpdatedAt=DateTime.UtcNow;await db.SaveChangesAsync();await Audit(input.IsActive?"UserActivated":"UserDisabled","User",id);return Ok(new{x.Id,x.IsActive});}
    [HttpDelete("users/{id:int}")]
    public async Task<IActionResult> DeleteUser(int id){if(id==AdminId)return Conflict(new{message="Kendi hesabınızı silemezsiniz."});var x=await db.AppUsers.FindAsync(id);if(x==null||x.IsDeleted)return NotFound(new{message="Kullanıcı bulunamadı."});x.IsDeleted=true;x.IsActive=false;x.DeletedAt=DateTime.UtcNow;x.UpdatedAt=DateTime.UtcNow;await db.SaveChangesAsync();await Audit("UserDeleted","User",id);return NoContent();}

    [HttpPut("businesses/{id:int}")]
    public async Task<IActionResult> Business(int id,BusinessUpdate v){var x=await db.Businesses.FindAsync(id);if(x==null||x.IsDeleted)return NotFound(new{message="İşletme bulunamadı."});if(string.IsNullOrWhiteSpace(v.Name)||v.CloseTime<=v.OpenTime)return BadRequest(new{message="İşletme adı ve çalışma saatleri geçerli olmalıdır."});if(v.OwnerUserId.HasValue&&!await db.AppUsers.AnyAsync(u=>u.Id==v.OwnerUserId&&!u.IsDeleted&&u.IsActive&&u.Role=="BusinessOwner"))return BadRequest(new{message="Geçerli bir işletme sahibi seçin."});x.Name=v.Name.Trim();x.Address=v.Address?.Trim();x.Phone=v.Phone?.Trim();x.OpenTime=v.OpenTime;x.CloseTime=v.CloseTime;x.OwnerUserId=v.OwnerUserId;x.IsActive=v.IsActive;await db.SaveChangesAsync();await Audit("BusinessUpdated","Business",id,v);return Ok(x);}
    [HttpPatch("businesses/{id:int}/status")] public async Task<IActionResult> BusinessStatus(int id,StatusUpdate v){var x=await db.Businesses.FindAsync(id);if(x==null||x.IsDeleted)return NotFound(new{message="İşletme bulunamadı."});x.IsActive=v.IsActive;await db.SaveChangesAsync();await Audit(v.IsActive?"BusinessActivated":"BusinessDisabled","Business",id);return Ok(new{x.Id,x.IsActive});}
    [HttpDelete("businesses/{id:int}")] public async Task<IActionResult> DeleteBusiness(int id){var x=await db.Businesses.FindAsync(id);if(x==null||x.IsDeleted)return NotFound(new{message="İşletme bulunamadı."});x.IsDeleted=true;x.IsActive=false;await db.SaveChangesAsync();await Audit("BusinessDeleted","Business",id);return NoContent();}

    [HttpPut("employees/{id:int}")]
    public async Task<IActionResult> Employee(int id,EmployeeUpdate v){var x=await db.Employees.FindAsync(id);if(x==null||x.IsDeleted)return NotFound(new{message="Çalışan bulunamadı."});if(string.IsNullOrWhiteSpace(v.FullName)||!await db.Businesses.AnyAsync(b=>b.Id==v.BusinessId&&!b.IsDeleted))return BadRequest(new{message="Ad soyad ve işletme geçerli olmalıdır."});x.FullName=v.FullName.Trim();x.BusinessId=v.BusinessId;x.IsActive=v.IsActive;await db.SaveChangesAsync();await Audit("EmployeeUpdated","Employee",id,v);return Ok(x);}
    [HttpPatch("employees/{id:int}/status")] public async Task<IActionResult> EmployeeStatus(int id,StatusUpdate v){var x=await db.Employees.FindAsync(id);if(x==null||x.IsDeleted)return NotFound(new{message="Çalışan bulunamadı."});x.IsActive=v.IsActive;await db.SaveChangesAsync();await Audit(v.IsActive?"EmployeeActivated":"EmployeeDisabled","Employee",id);return Ok(new{x.Id,x.IsActive});}
    [HttpDelete("employees/{id:int}")] public async Task<IActionResult> DeleteEmployee(int id){var x=await db.Employees.FindAsync(id);if(x==null||x.IsDeleted)return NotFound(new{message="Çalışan bulunamadı."});x.IsDeleted=true;x.IsActive=false;await db.SaveChangesAsync();await Audit("EmployeeDeleted","Employee",id);return NoContent();}

    [HttpPut("services/{id:int}")]
    public async Task<IActionResult> Service(int id,ServiceUpdate v){var x=await db.Services.FindAsync(id);if(x==null||x.IsDeleted)return NotFound(new{message="Hizmet bulunamadı."});if(string.IsNullOrWhiteSpace(v.Name)||v.Price<0||v.DurationMinutes<=0||v.BufferMinutes<0)return BadRequest(new{message="Hizmet bilgileri geçersiz."});x.Name=v.Name.Trim();x.Price=v.Price;x.DurationMinutes=v.DurationMinutes;x.BufferMinutes=v.BufferMinutes;x.IsActive=v.IsActive;await db.SaveChangesAsync();await Audit("ServiceUpdated","Service",id,v);return Ok(x);}
    [HttpPatch("services/{id:int}/status")] public async Task<IActionResult> ServiceStatus(int id,StatusUpdate v){var x=await db.Services.FindAsync(id);if(x==null||x.IsDeleted)return NotFound(new{message="Hizmet bulunamadı."});x.IsActive=v.IsActive;await db.SaveChangesAsync();await Audit(v.IsActive?"ServiceActivated":"ServiceDisabled","Service",id);return Ok(new{x.Id,x.IsActive});}
    [HttpDelete("services/{id:int}")] public async Task<IActionResult> DeleteService(int id){var x=await db.Services.FindAsync(id);if(x==null||x.IsDeleted)return NotFound(new{message="Hizmet bulunamadı."});x.IsDeleted=true;x.IsActive=false;await db.SaveChangesAsync();await Audit("ServiceDeleted","Service",id);return NoContent();}

    [HttpPost("employee-services")] public async Task<IActionResult> Assign(AssignmentCreate v){var employee=await db.Employees.FindAsync(v.EmployeeId);var service=await db.Services.FindAsync(v.ServiceId);if(employee==null||service==null||employee.IsDeleted||service.IsDeleted||employee.BusinessId!=service.BusinessId)return BadRequest(new{message="Çalışan ve hizmet aynı aktif işletmeye ait olmalıdır."});if(await db.EmployeeServices.AnyAsync(x=>x.EmployeeId==v.EmployeeId&&x.ServiceId==v.ServiceId))return Conflict(new{message="Bu hizmet zaten atanmış."});var x=new EmployeeService{EmployeeId=v.EmployeeId,ServiceId=v.ServiceId};db.Add(x);await db.SaveChangesAsync();await Audit("ServiceAssigned","EmployeeService",x.Id,v);return Ok(x);}
    [HttpDelete("employee-services/{id:int}")] public async Task<IActionResult> Unassign(int id){var x=await db.EmployeeServices.FindAsync(id);if(x==null)return NotFound(new{message="Atama bulunamadı."});db.Remove(x);await db.SaveChangesAsync();await Audit("ServiceUnassigned","EmployeeService",id);return NoContent();}

    [HttpPut("appointments/{id:int}/cancel")]
    public async Task<IActionResult> CancelAppointment(int id,CancelRequest v){var x=await db.Appointments.FindAsync(id);if(x==null)return NotFound(new{message="Randevu bulunamadı."});if(x.Status is "Completed" or "NoShow" or "CancelledRefunded" or "CancelledLate")return Conflict(new{message="Bu randevu iptal edilemez."});var now=DateTime.Now;if(now>=x.StartTime)return Conflict(new{message="Başlamış randevu iptal edilemez; gelmedi olarak işaretleyin."});x.CancelledAt=now;x.CancellationReason=v.Reason?.Trim();if(now<=x.StartTime.AddHours(-24)){x.Status="CancelledRefunded";x.PaymentStatus="DepositRefunded";}else{x.Status="CancelledLate";x.PaymentStatus="DepositKept";}await db.SaveChangesAsync();await Audit("AppointmentCancelled","Appointment",id,new{x.Status,x.PaymentStatus});return Ok(new{x.Id,x.Status,x.PaymentStatus});}
    [HttpPut("appointments/{id:int}/complete")] public async Task<IActionResult> Complete(int id){var x=await db.Appointments.FindAsync(id);if(x==null)return NotFound(new{message="Randevu bulunamadı."});if(x.Status!="Confirmed")return Conflict(new{message="Sadece onaylı randevu tamamlanabilir."});x.Status="Completed";x.PaymentStatus="Completed";x.CompletedAt=DateTime.Now;await db.SaveChangesAsync();await Audit("AppointmentCompleted","Appointment",id);return Ok(new{x.Id,x.Status});}
    [HttpPut("appointments/{id:int}/no-show")] public async Task<IActionResult> NoShow(int id){var x=await db.Appointments.FindAsync(id);if(x==null)return NotFound(new{message="Randevu bulunamadı."});if(x.Status!="Confirmed")return Conflict(new{message="Sadece onaylı randevu gelmedi olarak işaretlenebilir."});x.Status="NoShow";x.PaymentStatus="DepositKept";await db.SaveChangesAsync();await Audit("AppointmentNoShow","Appointment",id);return Ok(new{x.Id,x.Status});}

    [HttpPatch("reviews/{id:int}/publication")] public async Task<IActionResult> ReviewPublication(int id,PublicationUpdate v){var x=await db.Reviews.FindAsync(id);if(x==null)return NotFound(new{message="Yorum bulunamadı."});x.IsPublished=v.IsPublished;await db.SaveChangesAsync();await Audit(v.IsPublished?"ReviewPublished":"ReviewHidden","Review",id);return Ok(new{x.Id,x.IsPublished});}
    [HttpDelete("reviews/{id:int}")] public async Task<IActionResult> Review(int id){var x=await db.Reviews.FindAsync(id);if(x==null)return NotFound(new{message="Yorum bulunamadı."});db.Remove(x);await db.SaveChangesAsync();await Audit("ReviewDeleted","Review",id);return NoContent();}

    [HttpPost("content")] public async Task<IActionResult> Content(ContentItem x){if(string.IsNullOrWhiteSpace(x.Key)||string.IsNullOrWhiteSpace(x.Title))return BadRequest(new{message="Anahtar ve başlık zorunludur."});x.Id=0;x.CreatedAt=x.UpdatedAt=DateTime.UtcNow;db.Add(x);await db.SaveChangesAsync();await Audit("ContentCreated","Content",x.Id,x);return Ok(x);}
    [HttpPut("content/{id:int}")] public async Task<IActionResult> Content(int id,ContentItem v){var x=await db.ContentItems.FindAsync(id);if(x==null)return NotFound(new{message="İçerik bulunamadı."});x.Key=v.Key.Trim();x.Title=v.Title.Trim();x.Value=v.Value;x.ContentType=v.ContentType;x.Group=v.Group;x.ImageUrl=v.ImageUrl;x.IsActive=v.IsActive;x.UpdatedAt=DateTime.UtcNow;await db.SaveChangesAsync();await Audit("ContentUpdated","Content",id,x);return Ok(x);}
    [HttpDelete("content/{id:int}")] public async Task<IActionResult> Content(int id){var x=await db.ContentItems.FindAsync(id);if(x==null)return NotFound(new{message="İçerik bulunamadı."});db.Remove(x);await db.SaveChangesAsync();await Audit("ContentDeleted","Content",id);return NoContent();}
    [HttpPost("announcements")] public async Task<IActionResult> Announcement(Announcement x){if(string.IsNullOrWhiteSpace(x.Title))return BadRequest(new{message="Başlık zorunludur."});x.Id=0;x.CreatedAt=x.UpdatedAt=DateTime.UtcNow;db.Add(x);await db.SaveChangesAsync();await Audit("AnnouncementCreated","Announcement",x.Id,x);return Ok(x);}
    [HttpPut("announcements/{id:int}")] public async Task<IActionResult> Announcement(int id,Announcement v){var x=await db.Announcements.FindAsync(id);if(x==null)return NotFound(new{message="Duyuru bulunamadı."});x.Title=v.Title.Trim();x.Body=v.Body;x.StartAt=v.StartAt;x.EndAt=v.EndAt;x.IsActive=v.IsActive;x.UpdatedAt=DateTime.UtcNow;await db.SaveChangesAsync();await Audit("AnnouncementUpdated","Announcement",id,x);return Ok(x);}
    [HttpDelete("announcements/{id:int}")] public async Task<IActionResult> Announcement(int id){var x=await db.Announcements.FindAsync(id);if(x==null)return NotFound(new{message="Duyuru bulunamadı."});db.Remove(x);await db.SaveChangesAsync();await Audit("AnnouncementDeleted","Announcement",id);return NoContent();}
    [HttpPost("faq")] public async Task<IActionResult> Faq(FaqItem x){if(string.IsNullOrWhiteSpace(x.Question)||string.IsNullOrWhiteSpace(x.Answer))return BadRequest(new{message="Soru ve cevap zorunludur."});x.Id=0;x.CreatedAt=x.UpdatedAt=DateTime.UtcNow;db.Add(x);await db.SaveChangesAsync();await Audit("FaqCreated","Faq",x.Id,x);return Ok(x);}
    [HttpPut("faq/{id:int}")] public async Task<IActionResult> Faq(int id,FaqItem v){var x=await db.FaqItems.FindAsync(id);if(x==null)return NotFound(new{message="Soru bulunamadı."});x.Question=v.Question.Trim();x.Answer=v.Answer.Trim();x.SortOrder=v.SortOrder;x.IsActive=v.IsActive;x.UpdatedAt=DateTime.UtcNow;await db.SaveChangesAsync();await Audit("FaqUpdated","Faq",id,x);return Ok(x);}
    [HttpDelete("faq/{id:int}")] public async Task<IActionResult> Faq(int id){var x=await db.FaqItems.FindAsync(id);if(x==null)return NotFound(new{message="Soru bulunamadı."});db.Remove(x);await db.SaveChangesAsync();await Audit("FaqDeleted","Faq",id);return NoContent();}
    [HttpPut("settings/{id:int}")] public async Task<IActionResult> Setting(int id,SettingUpdate v){var x=await db.AppSettings.FindAsync(id);if(x==null||SecretKey(x.Key))return NotFound(new{message="Ayar bulunamadı."});x.Value=v.Value;x.UpdatedAt=DateTime.UtcNow;await db.SaveChangesAsync();await Audit("SettingUpdated","Setting",id,new{x.Key,x.Value});return Ok(x);}
    [HttpPost("media"),RequestSizeLimit(10_000_000)] public async Task<IActionResult> Media(IFormFile file){if(file.Length==0||file.Length>10_000_000)return BadRequest(new{message="Geçersiz dosya."});var allowed=new[]{"image/jpeg","image/png","image/webp","application/pdf"};if(!allowed.Contains(file.ContentType))return BadRequest(new{message="Desteklenmeyen dosya türü."});var dir=Path.Combine(env.WebRootPath??Path.Combine(env.ContentRootPath,"wwwroot"),"media");Directory.CreateDirectory(dir);var name=Guid.NewGuid().ToString("N")+Path.GetExtension(file.FileName);await using(var stream=System.IO.File.Create(Path.Combine(dir,name)))await file.CopyToAsync(stream);var x=new MediaAsset{FileName=name,OriginalFileName=Path.GetFileName(file.FileName),Url="/media/"+name,MimeType=file.ContentType,SizeBytes=file.Length,UploadedByAdminUserId=AdminId};db.Add(x);await db.SaveChangesAsync();await Audit("MediaUploaded","Media",x.Id);return Ok(x);}
    [HttpDelete("media/{id:int}")] public async Task<IActionResult> DeleteMedia(int id){var x=await db.MediaAssets.FindAsync(id);if(x==null)return NotFound(new{message="Medya bulunamadı."});var root=Path.GetFullPath(env.WebRootPath??Path.Combine(env.ContentRootPath,"wwwroot"));var path=Path.GetFullPath(Path.Combine(root,"media",x.FileName));if(path.StartsWith(root,StringComparison.OrdinalIgnoreCase)&&System.IO.File.Exists(path))System.IO.File.Delete(path);db.Remove(x);await db.SaveChangesAsync();await Audit("MediaDeleted","Media",id);return NoContent();}
}
public sealed record UserCreate(string FullName,string Email,string Phone,string Password,string Role,bool IsActive);
public sealed record BusinessCreate(string Name,string? Address,string? Phone,TimeSpan OpenTime,TimeSpan CloseTime,int OwnerUserId,bool IsActive);
public sealed record EmployeeCreate(string FullName,int BusinessId,bool IsActive);
public sealed record ServiceCreate(string Name,int BusinessId,decimal Price,int DurationMinutes,int BufferMinutes,bool IsActive);
public sealed record UserUpdate(string FullName,string Email,string Phone,string Role,bool IsActive);
public sealed record StatusUpdate(bool IsActive);
public sealed record PublicationUpdate(bool IsPublished);
public sealed record BusinessUpdate(string Name,string? Address,string? Phone,TimeSpan OpenTime,TimeSpan CloseTime,int? OwnerUserId,bool IsActive);
public sealed record EmployeeUpdate(string FullName,int BusinessId,bool IsActive);
public sealed record ServiceUpdate(string Name,decimal Price,int DurationMinutes,int BufferMinutes,bool IsActive);
public sealed record AssignmentCreate(int EmployeeId,int ServiceId);
public sealed record CancelRequest(string? Reason);
public sealed record SettingUpdate(string Value);
