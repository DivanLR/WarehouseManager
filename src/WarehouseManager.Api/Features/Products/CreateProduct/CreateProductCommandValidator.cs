using FluentValidation;

namespace WarehouseManager.Api.Features.Products.CreateProduct;

internal sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(command => command.Code).NotEmpty().MaximumLength(50);
        RuleFor(command => command.Description).NotEmpty().MaximumLength(255);
    }
}
