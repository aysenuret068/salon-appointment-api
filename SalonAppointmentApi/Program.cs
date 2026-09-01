using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SalonAppointmentApi.Auth;
using SalonAppointmentApi.Data;

var builder = WebApplication.CreateBuilder(args);

// Render tarafından verilen PORT değerini kullanır.
// Bilgisayarda çalıştırırken launchSettings kullanılmaya devam eder.
var renderPort = Environment.GetEnvironmentVariable("PORT");

if (!string.IsNullOrWhiteSpace(renderPort))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{renderPort}");
}

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var jwtOptions = jwtSection.Get<JwtOptions>()
    ?? throw new InvalidOperationException("JWT configuration is missing.");
if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey) || jwtOptions.SigningKey.Length < 32)
    throw new InvalidOperationException("JWT signing key must contain at least 32 characters.");

builder.Services.Configure<JwtOptions>(jwtSection);
builder.Services.AddScoped<ITokenService, JwtTokenService>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtOptions.Audience,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
        ClockSkew = TimeSpan.FromMinutes(1)
    });
builder.Services.AddAuthorization();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy("AppCors", policy =>
{
    if (allowedOrigins.Length == 0 && builder.Environment.IsDevelopment()) policy.AllowAnyOrigin();
    else policy.WithOrigins(allowedOrigins);
    policy.AllowAnyHeader().AllowAnyMethod();
}));

var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "DefaultConnection bağlantı bilgisi bulunamadı."
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

app.UseExceptionHandler();

// TiDB üzerinde eksik tabloları migration dosyalarından oluşturur.
using (var scope = app.Services.CreateScope())
{
    var dbContext =
        scope.ServiceProvider.GetRequiredService<AppDbContext>();

    dbContext.Database.Migrate();
}

app.UseSwagger();
app.UseSwaggerUI();
app.UseStaticFiles();

// Render HTTPS işlemini kendisi yönettiği için burada
// app.UseHttpsRedirection() kullanmıyoruz.

app.UseCors("AppCors");

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/", () => Results.Ok(
    new
    {
        message = "Salon Appointment API çalışıyor."
    }
));



app.Run();
