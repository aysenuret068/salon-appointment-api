namespace SalonAppointmentApi.DTOs
{
    public class CreateServiceRequest
    {
        public int BusinessId { get; set; }
        public string Name { get; set; } = "";

        public int DurationMinutes { get; set; }
        public int BufferMinutes { get; set; }

        public decimal Price { get; set; }
    }
}
