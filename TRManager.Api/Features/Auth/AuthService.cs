using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TRManager.Api.Data;
using TRManager.Api.Data.Entities;
using TRManager.Api.Features.Auth.Dtos;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest req);
    Task<AuthResponse> LoginAsync(LoginRequest req, string? ip = null);
    Task<AuthResponse> RefreshAsync(string refreshToken);
    Task LogoutAsync(string refreshToken);
}

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _db;
    private readonly IPasswordHasher<User> _hasher;
    private readonly IJwtTokenGenerator _jwt;
    private readonly IConfiguration _config;

    public AuthService(ApplicationDbContext db, IPasswordHasher<User> hasher, IJwtTokenGenerator jwt, IConfiguration config)
    {
        _db = db; _hasher = hasher; _jwt = jwt; _config = config;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest req)
    {
        if (await _db.Users.AnyAsync(x => x.Email == req.Email))
            throw new InvalidOperationException("Email already exists.");

        var user = new User
        {
            Email = req.Email,
            UserName = req.UserName,
            FullName = req.FullName,
            Phone = req.Phone
        };
        user.PasswordHash = _hasher.HashPassword(user, req.Password);

        // mặc định role "User" nếu muốn: (tùy bước 4 seeding)
        await _db.Users.AddAsync(user);
        await _db.SaveChangesAsync();

        // phát token luôn
        var (access, exp, jti) = _jwt.CreateAccessToken(user, roles: Array.Empty<string>());
        var refresh = await SaveRefreshToken(user, jti);
        return new AuthResponse(access, refresh.Token, exp);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest req, string? ip = null)
    {
        var user = await _db.Users.Include(u => u.Roles).FirstOrDefaultAsync(x => x.Email == req.Email);
        if (user is null) throw new UnauthorizedAccessException("Invalid credentials.");

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, req.Password);
        if (result == PasswordVerificationResult.Failed)
            throw new UnauthorizedAccessException("Invalid credentials.");

        var roles = user.Roles.Select(r => r.Name);
        var (access, exp, jti) = _jwt.CreateAccessToken(user, roles);
        var refresh = await SaveRefreshToken(user, jti, ip);

        return new AuthResponse(access, refresh.Token, exp);
    }

    public async Task<AuthResponse> RefreshAsync(string refreshToken)
    {
        var token = await _db.RefreshTokens.Include(r => r.User)
            .FirstOrDefaultAsync(t => t.Token == refreshToken);

        if (token is null || token.RevokedAt != null || token.ExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Refresh token is invalid.");

        // (tùy chọn) rotate: revoke token cũ
        token.RevokedAt = DateTime.UtcNow;
        _db.RefreshTokens.Update(token);

        var user = token.User!;
        var roles = user.Roles.Select(r => r.Name);
        var (access, exp, jti) = _jwt.CreateAccessToken(user, roles);
        var newRefresh = await SaveRefreshToken(user, jti);

        await _db.SaveChangesAsync();
        return new AuthResponse(access, newRefresh.Token, exp);
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var token = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == refreshToken);
        if (token != null)
        {
            token.RevokedAt = DateTime.UtcNow;
            _db.RefreshTokens.Update(token);
            await _db.SaveChangesAsync();
        }
    }

    private async Task<RefreshToken> SaveRefreshToken(User user, string? jti, string? ip = null)
    {
        var days = int.Parse(_config["Jwt:RefreshTokenDays"] ?? "14");
        var refresh = new RefreshToken
        {
            UserId = user.Id,
            JwtId = jti,
            Token = _jwt.CreateRefreshToken(),
            ExpiresAt = DateTime.UtcNow.AddDays(days),
            CreatedByIp = ip
        };
        await _db.RefreshTokens.AddAsync(refresh);
        await _db.SaveChangesAsync();
        return refresh;
    }
}
