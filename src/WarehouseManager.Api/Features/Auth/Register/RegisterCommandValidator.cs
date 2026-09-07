using FluentValidation;

namespace WarehouseManager.Api.Features.Auth.Register;

internal sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(command => command.Email).NotEmpty().EmailAddress().MaximumLength(255);
        RuleFor(command => command.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
    }
}
