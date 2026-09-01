using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SalonAppointmentApi.Models;

public class Review
{
    public int Id { get; set; }

    [Required]
    public int AppointmentId { get; set; }

    [ForeignKey(nameof(AppointmentId))]
    public Appointment? Appointment { get; set; }

    [Required]
    public int BusinessId { get; set; }

    [ForeignKey(nameof(BusinessId))]
    public Business? Business { get; set; }

    [Required]
    public int CustomerId { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public AppUser? Customer { get; set; }

    public int? EmployeeId { get; set; }

    [ForeignKey(nameof(EmployeeId))]
    public Employee? Employee { get; set; }

    [Required]
    [Range(1, 5)]
    public int Rating { get; set; }

    [MaxLength(500)]
    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsPublished { get; set; } = true;
}