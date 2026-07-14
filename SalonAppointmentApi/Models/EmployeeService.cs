namespace SalonAppointmentApi.Models
{
    public class EmployeeService
    {
        public int Id { get; set; }

        public int EmployeeId { get; set; }
        public int ServiceId { get; set; }

        public Employee? Employee { get; set; }
        public ServiceItem? Service { get; set; }
    }
}
