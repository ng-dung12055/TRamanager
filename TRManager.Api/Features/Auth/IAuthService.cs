using TRManager.Api.Features.Auth.Dtos;

namespace TRManager.Api.Features.Auth;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest req, string? ip = null);
    Task<AuthResponse> LoginAsync(LoginRequest req, string? ip = null);
    Task<AuthResponse> RefreshAsync(string refreshToken, string? ip = null);
    Task LogoutAsync(string refreshToken, string? ip = null);
}
