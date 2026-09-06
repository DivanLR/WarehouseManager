using FluentValidation;

namespace WarehouseManager.Api.Features.Stock.AddStock;

internal sealed class AddStockCommandValidator : AbstractValidator<AddStockCommand>
{
    public AddStockCommandValidator()
    {
        RuleFor(command => command.WarehouseCode).NotEmpty().MaximumLength(50);
        RuleFor(command => command.ProductCode).NotEmpty().MaximumLength(50);
        RuleFor(command => command.Quantity).GreaterThan(0);
    }
}
