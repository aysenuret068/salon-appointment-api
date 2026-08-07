using Microsoft.EntityFrameworkCore;
using SalonAppointmentApi.Data;

var builder = WebApplication.CreateBuilder(args);

// Render tarafýndan verilen PORT deðerini kullanýr.
// Bilgisayarda çalýþtýrýrken launchSettings kullanýlmaya devam eder.
var renderPort = Environment.GetEnvironmentVariable("PORT");

if (!string.IsNullOrWhiteSpace(renderPort))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{renderPort}");
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "DefaultConnection baðlantý bilgisi bulunamadý."
    );
}

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseMySql(
        connectionString,
        new MySqlServerVersion(new Version(8, 0, 11))
    );
});

var app = builder.Build();

// TiDB üzerinde eksik tablolarý migration dosyalarýndan oluþturur.
using (var scope = app.Services.CreateScope())
{
    var dbContext =
        scope.ServiceProvider.GetRequiredService<AppDbContext>();

    dbContext.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI();

// Render HTTPS iþlemini kendisi yönettiði için burada
// app.UseHttpsRedirection() kullanmýyoruz.

app.UseCors("AllowAll");

app.UseAuthorization();
app.MapControllers();

app.MapGet("/", () => Results.Ok(
    new
    {
        message = "Salon Appointment API çalýþýyor."
    }
));



app.Run();  