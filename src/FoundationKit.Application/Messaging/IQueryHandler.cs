using FoundationKit.Application.Results;

namespace FoundationKit.Application.Messaging;

public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    Task<Result<TResponse>> HandleAsync(
        TQuery query,
        CancellationToken cancellationToken = default);
}
