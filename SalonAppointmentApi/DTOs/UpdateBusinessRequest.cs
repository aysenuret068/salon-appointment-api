namespace SalonAppointmentApi.DTOs;

public class UpdateBusinessRequest
{
    public string Name { get; set; } = "";
    public string? Address { get; set; }
    public string? Phone { get; set; }

    public TimeSpan OpenTime { get; set; }
    public TimeSpan CloseTime { get; set; }
}