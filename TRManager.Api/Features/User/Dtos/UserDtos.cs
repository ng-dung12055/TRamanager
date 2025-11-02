using System;

namespace TRManager.Api.Features.User.Dtos
{
    public record UserDto(Guid Id, string Email, string UserName, string? FullName, string? Phone, bool IsActive, DateTime CreatedAt);
    public record UpdateUserRequest(string? FullName, string? Phone, bool? IsActive);
}
