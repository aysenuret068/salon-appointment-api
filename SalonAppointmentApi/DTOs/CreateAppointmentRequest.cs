namespace SalonAppointmentApi.DTOs;

public class CreateAppointmentRequest
{
    public int BusinessId { get; set; }
    public int EmployeeId { get; set; }
    public int ServiceId { get; set; }

    public int? CustomerUserId { get; set; }

    public string CustomerName { get; set; } = "";
    public string CustomerPhone { get; set; } = "";

    public DateTime StartTime { get; set; }
}