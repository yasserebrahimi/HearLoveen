using MediatR;

namespace HearLoveen.Shared.Application.Abstractions;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}
