using FluentValidation.Results;
using Shouldly;
using WarehouseManager.Api.Features.Products.CreateProduct;

namespace WarehouseManager.UnitTests.Products;

public class CreateProductCommandValidatorTests
{
    private readonly CreateProductCommandValidator _validator = new();

    [Fact]
    public void Validate_Should_Fail_WhenCodeIsEmpty()
    {
        var command = new CreateProductCommand(string.Empty, "A description");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_Should_Fail_WhenDescriptionIsEmpty()
    {
        var command = new CreateProductCommand("SKU-001", string.Empty);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void Validate_Should_Succeed_WhenFieldsAreValid()
    {
        var command = new CreateProductCommand("SKU-001", "A description");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.ShouldBeTrue();
    }
}
