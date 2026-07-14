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

    public AppUser? OwnerUser { get; set; }

    public List<Employee> Employees { get; set; } = new();
    public List<ServiceItem> Services { get; set; } = new();
}
