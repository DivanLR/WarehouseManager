using FluentValidation.Results;
using Shouldly;
using WarehouseManager.Api.Features.Stock.AddStock;

namespace WarehouseManager.UnitTests.Stock;

public class AddStockCommandValidatorTests
{
    private readonly AddStockCommandValidator _validator = new();

    [Fact]
    public void Validate_Should_Fail_WhenWarehouseCodeIsEmpty()
    {
        var command = new AddStockCommand(string.Empty, "SKU-001", 10);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_Should_Fail_WhenProductCodeIsEmpty()
    {
        var command = new AddStockCommand("WH-001", string.Empty, 10);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Validate_Should_Fail_WhenQuantityIsNotPositive(int quantity)
    {
        var command = new AddStockCommand("WH-001", "SKU-001", quantity);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_Should_Succeed_WhenFieldsAreValid()
    {
        var command = new AddStockCommand("WH-001", "SKU-001", 10);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeTrue();
    }
}
