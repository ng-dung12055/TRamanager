namespace TRManager.Api.Features.Auth.Dtos
{
    public record RegisterRequest(string Email, string UserName, string Password, string? FullName, string? Phone);
    public record LoginRequest(string Email, string Password);
    public record AuthResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);
}
