using FluentValidation.Results;
using Shouldly;
using WarehouseManager.Api.Features.Orders.CreateOrder;

namespace WarehouseManager.UnitTests.Orders;

public class CreateOrderCommandValidatorTests
{
    private readonly CreateOrderCommandValidator _validator = new();

    [Fact]
    public void Validate_Should_Fail_WhenProductCodeIsEmpty()
    {
        var command = new CreateOrderCommand(string.Empty, "WH-001", "WH-002", 10);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_Should_Fail_WhenSourceEqualsDestination()
    {
        var command = new CreateOrderCommand("SKU-001", "WH-001", "WH-001", 10);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage == "Source and destination warehouses must differ.");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_Should_Fail_WhenQuantityIsNotPositive(int quantity)
    {
        var command = new CreateOrderCommand("SKU-001", "WH-001", "WH-002", quantity);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_Should_Succeed_WhenFieldsAreValid()
    {
        var command = new CreateOrderCommand("SKU-001", "WH-001", "WH-002", 10);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeTrue();
    }
}
