using Microsoft.EntityFrameworkCore;
using SalonAppointmentApi.Models;

namespace SalonAppointmentApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<ServiceItem> Services => Set<ServiceItem>();
    public DbSet<EmployeeService> EmployeeServices => Set<EmployeeService>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Review> Reviews => Set<Review>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>()
            .HasIndex(x => x.Email)
            .IsUnique();

        modelBuilder.Entity<ServiceItem>()
            .Property(x => x.Price)
            .HasPrecision(10, 2);

        modelBuilder.Entity<Appointment>()
            .Property(x => x.TotalPrice)
            .HasPrecision(10, 2);

        modelBuilder.Entity<Appointment>()
            .Property(x => x.DepositAmount)
            .HasPrecision(10, 2);

        modelBuilder.Entity<Appointment>()
            .Property(x => x.RemainingAmount)
            .HasPrecision(10, 2);

        modelBuilder.Entity<EmployeeService>()
            .HasIndex(x => new { x.EmployeeId, x.ServiceId })
            .IsUnique();

        modelBuilder.Entity<Business>()
            .HasOne(x => x.OwnerUser)
            .WithMany()
            .HasForeignKey(x => x.OwnerUserId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Appointment>()
            .HasOne(x => x.CustomerUser)
            .WithMany()
            .HasForeignKey(x => x.CustomerUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}