namespace SalonAppointmentApi.Models
{
    public class ServiceItem
    {

        public int Id { get; set; }

        public int BusinessId { get; set; }

        public string Name { get; set; } = "";

        public int DurationMinutes { get; set; }

        public int BufferMinutes { get; set; }

        public decimal Price { get; set; }

        public Business? Business { get; set; }
    }
}
