using FluentValidation;
using TRManager.Api.Features.Auth.Dtos;

namespace TRManager.Api.Features.Auth.Validation;

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();

        RuleFor(x => x.NewPassword)
            .NotEmpty().MinimumLength(8)
            .Matches("[A-Z]").WithMessage("Phải có chữ HOA.")
            .Matches("[a-z]").WithMessage("Phải có chữ thường.")
            .Matches("[0-9]").WithMessage("Phải có số.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Phải có ký tự đặc biệt.");
    }
}
