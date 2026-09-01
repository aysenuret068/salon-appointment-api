using SalonAppointmentApi.Models;

namespace SalonAppointmentApi.Auth;

public interface ITokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateAccessToken(AppUser user);
}
