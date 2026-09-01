namespace SalonAppointmentApi.DTOs;

public class LoginResponse
{
    public string AccessToken { get; set; } = "";
    public DateTime ExpiresAtUtc { get; set; }
    public int UserId { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Role { get; set; } = "";
}
