using FluentValidation;

namespace WarehouseManager.Api.Features.Warehouses.CreateWarehouse;

internal sealed class CreateWarehouseCommandValidator : AbstractValidator<CreateWarehouseCommand>
{
    public CreateWarehouseCommandValidator()
    {
        RuleFor(command => command.Code).NotEmpty().MaximumLength(50);
        RuleFor(command => command.Name).NotEmpty().MaximumLength(255);
    }
}
