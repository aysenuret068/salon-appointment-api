namespace SalonAppointmentApi.Models
{
    public class Employee
    {
        public int Id { get; set; }

        public int BusinessId { get; set; }
        public string FullName { get; set; } = "";
        public bool IsActive { get; set; } = true;

        public Business? Business { get; set; }

        public List<EmployeeService> EmployeeServices { get; set; } = new();
    }
}
