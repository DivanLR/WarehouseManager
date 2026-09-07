using FluentValidation;

namespace WarehouseManager.Api.Features.Orders.CreateOrder;

internal sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(command => command.ProductCode).NotEmpty().MaximumLength(50);
        RuleFor(command => command.SourceWarehouseCode).NotEmpty().MaximumLength(50);
        RuleFor(command => command.DestinationWarehouseCode)
            .NotEmpty()
            .MaximumLength(50)
            .NotEqual(command => command.SourceWarehouseCode)
            .WithMessage("Source and destination warehouses must differ.");
        RuleFor(command => command.Quantity).GreaterThan(0);
    }
}
