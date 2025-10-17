using OnlineCinema.API.DTOs;
using OnlineCinema.API.Models;

namespace OnlineCinema.API.Services;

public interface IAuthService
{
    Task<LoginResponse> RegisterAsync(RegisterRequest request);
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<bool> ValidateUserAsync(string email, string password);
    Task<User?> GetUserByEmailAsync(string email);
    string GenerateJwtToken(User user);
}
