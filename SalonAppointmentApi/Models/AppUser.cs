namespace SalonAppointmentApi.Models;

public class AppUser
{
    public int Id { get; set; }

    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";

    public string PasswordHash { get; set; } = "";

    public string Role { get; set; } = "Customer";

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public DateTime? DeletedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
}
