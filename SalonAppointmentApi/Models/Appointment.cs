namespace SalonAppointmentApi.Models;

public class Appointment
{
    public int Id { get; set; }

    public int BusinessId { get; set; }
    public int EmployeeId { get; set; }
    public int ServiceId { get; set; }

    public int? CustomerUserId { get; set; }

    public string CustomerName { get; set; } = "";
    public string CustomerPhone { get; set; } = "";

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public decimal TotalPrice { get; set; }
    public decimal DepositAmount { get; set; }
    public decimal RemainingAmount { get; set; }

    public string Status { get; set; } = "Confirmed";
    public string PaymentStatus { get; set; } = "DepositPaid";

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? PaidAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public string? CancellationReason { get; set; }

    public AppUser? CustomerUser { get; set; }
}