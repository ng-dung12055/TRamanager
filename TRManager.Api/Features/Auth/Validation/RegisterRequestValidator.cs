using FluentValidation;
using TRManager.Api.Features.Auth.Dtos;

namespace TRManager.Api.Features.Auth.Validation;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được trống")
            .EmailAddress().WithMessage("Email không đúng định dạng");

        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("UserName không được trống")
            .MinimumLength(4).WithMessage("UserName tối thiểu 4 ký tự");

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8).WithMessage("Mật khẩu tối thiểu 8 ký tự")
            .Matches("[A-Z]").WithMessage("Phải có ít nhất 1 chữ HOA")
            .Matches("[a-z]").WithMessage("Phải có ít nhất 1 chữ thường")
            .Matches("[0-9]").WithMessage("Phải có ít nhất 1 số")
            .Matches("[^a-zA-Z0-9]").WithMessage("Phải có ít nhất 1 ký tự đặc biệt");

        // 👇 Thêm rule cho ConfirmPassword
        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Xác nhận mật khẩu không được trống")
            .Equal(x => x.Password).WithMessage("Xác nhận mật khẩu không khớp");

        RuleFor(x => x.Phone)
            .Matches(@"^\+?\d{9,15}$")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone))
            .WithMessage("Số điện thoại không hợp lệ");

        RuleFor(x => x.Role)
            .Must(r => string.IsNullOrEmpty(r) || new[] { "Admin", "Staff", "Tenant" }.Contains(r))
            .WithMessage("Role chỉ chấp nhận: Admin/Staff/Tenant");
    }
}
