using FluentValidation;

namespace WarehouseManager.Api.Features.Auth.Login;

internal sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(command => command.Email).NotEmpty().MaximumLength(255);
        RuleFor(command => command.Password).NotEmpty().MaximumLength(128);
    }
}
