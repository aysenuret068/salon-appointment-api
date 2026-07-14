using System.ComponentModel.DataAnnotations;

namespace SalonAppointmentApi.Dtos
{
    public class CreateReviewRequest
    {
        [Required]
        public int AppointmentId { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [MaxLength(500)]
        public string? Comment { get; set; }
    }
}
