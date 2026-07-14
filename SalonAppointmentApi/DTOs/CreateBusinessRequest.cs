namespace SalonAppointmentApi.DTOs;

public class CreateBusinessRequest
{
    public int? OwnerUserId { get; set; }

    public string Name { get; set; } = "";
    public string? Address { get; set; }
    public string? Phone { get; set; }

    public TimeSpan OpenTime { get; set; }
    public TimeSpan CloseTime { get; set; }
}
