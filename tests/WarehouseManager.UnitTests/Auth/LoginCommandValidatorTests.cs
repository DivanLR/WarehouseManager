using FluentValidation.Results;
using Shouldly;
using WarehouseManager.Api.Features.Auth.Login;

namespace WarehouseManager.UnitTests.Auth;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public void Validate_Should_Fail_WhenEmailIsEmpty()
    {
        var command = new LoginCommand(string.Empty, "a-valid-password");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_Should_Fail_WhenPasswordIsEmpty()
    {
        var command = new LoginCommand("user@example.com", string.Empty);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_Should_Succeed_WhenFieldsAreValid()
    {
        var command = new LoginCommand("user@example.com", "a-valid-password");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeTrue();
    }
}
