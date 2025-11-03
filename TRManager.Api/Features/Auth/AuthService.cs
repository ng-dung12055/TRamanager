using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TRManager.Api.Data;
using TRManager.Api.Data.Entities;
using TRManager.Api.Features.Auth.Dtos;

// ⚡ Alias để tránh trùng namespace TRManager.Api.Features.User
using AppUser = TRManager.Api.Data.Entities.User;

namespace TRManager.Api.Features.Auth;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _db;
    private readonly IPasswordHasher<AppUser> _hasher;
    private readonly IJwtTokenGenerator _jwt;

    public AuthService(
        ApplicationDbContext db,
        IPasswordHasher<AppUser> hasher,
        IJwtTokenGenerator jwt)
    {
        _db = db;
        _hasher = hasher;
        _jwt = jwt;
    }

    // ---------- Đăng ký ----------
    public async Task<AuthResponse> RegisterAsync(RegisterRequest req, string? ip = null)
    {
        if (await _db.Users.AnyAsync(u => u.Email == req.Email))
            throw new InvalidOperationException("Email đã tồn tại.");

        // Gán mặc định role = Tenant nếu không có
        var defaultRoleName = string.IsNullOrWhiteSpace(req.Role) ? "Tenant" : req.Role!;
        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name == defaultRoleName);
        if (role == null)
        {
            role = new Role { Name = defaultRoleName };
            _db.Roles.Add(role);
            await _db.SaveChangesAsync();
        }

        var user = new AppUser
        {
            Email = req.Email,
            UserName = req.UserName,
            FullName = req.FullName ?? "",
            Phone = req.Phone ?? "",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            Roles = new List<Role> { role }
        };
        user.PasswordHash = _hasher.HashPassword(user, req.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var (accessToken, expiresAt) = _jwt.Generate(user, user.Roles.Select(r => r.Name));

        var refresh = BuildRefreshToken(user.Id, ip);
        _db.RefreshTokens.Add(refresh);
        await _db.SaveChangesAsync();

        return new AuthResponse(accessToken, refresh.Token, expiresAt);
    }

    // ---------- Đăng nhập ----------
    public async Task<AuthResponse> LoginAsync(LoginRequest req, string? ip = null)
    {
        var user = await _db.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Email == req.Email);

        if (user == null)
            throw new InvalidOperationException("Sai tài khoản hoặc mật khẩu.");

        var verify = _hasher.VerifyHashedPassword(user, user.PasswordHash!, req.Password);
        if (verify == PasswordVerificationResult.Failed)
            throw new InvalidOperationException("Sai tài khoản hoặc mật khẩu.");

        var (accessToken, expiresAt) = _jwt.Generate(user, user.Roles.Select(r => r.Name));

        var refresh = BuildRefreshToken(user.Id, ip);
        _db.RefreshTokens.Add(refresh);
        await _db.SaveChangesAsync();

        return new AuthResponse(accessToken, refresh.Token, expiresAt);
    }

    // ---------- Làm mới token ----------
    public async Task<AuthResponse> RefreshAsync(string refreshToken, string? ip = null)
    {
        var token = await _db.RefreshTokens
            .Include(t => t.User)
            .ThenInclude(u => u!.Roles)
            .FirstOrDefaultAsync(t => t.Token == refreshToken);

        if (token == null || token.RevokedAt != null || token.ExpiresAt <= DateTime.UtcNow)
            throw new InvalidOperationException("Refresh token không hợp lệ hoặc đã hết hạn.");

        token.RevokedAt = DateTime.UtcNow;

        var user = token.User!;
        var (accessToken, expiresAt) = _jwt.Generate(user, user.Roles.Select(r => r.Name));

        var newRefresh = BuildRefreshToken(user.Id, ip);
        _db.RefreshTokens.Add(newRefresh);

        await _db.SaveChangesAsync();

        return new AuthResponse(accessToken, newRefresh.Token, expiresAt);
    }

    // ---------- Đăng xuất ----------
    public async Task LogoutAsync(string refreshToken, string? ip = null)
    {
        var token = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == refreshToken);
        if (token == null) return;

        token.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    // ---------- Tạo RefreshToken ----------
    private static RefreshToken BuildRefreshToken(Guid userId, string? ip)
        => new()
        {
            UserId = userId,
            Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                        .Replace("+", "").Replace("/", "").Replace("=", ""),
            ExpiresAt = DateTime.UtcNow.AddDays(14),
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = ip
        };
}
