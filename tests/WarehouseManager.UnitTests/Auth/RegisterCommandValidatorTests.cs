using FluentValidation.Results;
using Shouldly;
using WarehouseManager.Api.Features.Auth.Register;

namespace WarehouseManager.UnitTests.Auth;

public class RegisterCommandValidatorTests
{
    private readonly RegisterCommandValidator _validator = new();

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Validate_Should_Fail_WhenEmailIsNotAnAddress(string email)
    {
        var command = new RegisterCommand(email, "a-valid-password");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    public void Validate_Should_Fail_WhenPasswordIsTooShort(string password)
    {
        var command = new RegisterCommand("user@example.com", password);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_Should_ReportThePropertyName_SoAClientCanMapIt()
    {
        var command = new RegisterCommand(string.Empty, "a-valid-password");

        ValidationResult result = _validator.Validate(command);

        result.Errors.Select(e => e.PropertyName).ShouldContain("Email");
    }

    [Fact]
    public void Validate_Should_Succeed_WhenFieldsAreValid()
    {
        var command = new RegisterCommand("user@example.com", "a-valid-password");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeTrue();
    }
}
