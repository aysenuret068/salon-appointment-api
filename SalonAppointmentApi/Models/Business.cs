namespace SalonAppointmentApi.Models;

public class Business
{
    public int Id { get; set; }

    public int? OwnerUserId { get; set; }

    public string Name { get; set; } = "";
    public string? Address { get; set; }
    public string? Phone { get; set; }

    public TimeSpan OpenTime { get; set; }
    public TimeSpan CloseTime { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public AppUser? OwnerUser { get; set; }

    public List<Employee> Employees { get; set; } = new();
    public List<ServiceItem> Services { get; set; } = new();
}
