namespace TRManager.Api.Features.Auth.Dtos;

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = default!;
    public string NewPassword     { get; set; } = default!;
}
