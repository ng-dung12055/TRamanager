namespace TRManager.Api.Features.Auth.Dtos;
public class RegisterRequest
{
    public string Email { get; set; } = default!;
    public string UserName { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string ConfirmPassword { get; set; } = default!;   // thêm dòng này

    public string? FullName { get; set; }
    public string? Phone { get; set; }
    /// <summary>Admin/Staff/Tenant (mặc định: Tenant)</summary>
    public string? Role { get; set; }
}
