using WarehouseManager.Api.SharedModels;

namespace WarehouseManager.Api.Abstract;

public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken);
}
