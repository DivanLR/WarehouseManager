using FluentValidation.Results;
using Shouldly;
using WarehouseManager.Api.Features.Warehouses.CreateWarehouse;

namespace WarehouseManager.UnitTests.Warehouses;

public class CreateWarehouseCommandValidatorTests
{
    private readonly CreateWarehouseCommandValidator _validator = new();

    [Fact]
    public void Validate_Should_Fail_WhenCodeIsEmpty()
    {
        var command = new CreateWarehouseCommand(string.Empty, "Main depot");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_Should_Fail_WhenNameIsEmpty()
    {
        var command = new CreateWarehouseCommand("WH-001", string.Empty);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_Should_Succeed_WhenFieldsAreValid()
    {
        var command = new CreateWarehouseCommand("WH-001", "Main depot");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeTrue();
    }
}
