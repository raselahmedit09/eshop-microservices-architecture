using MediatR;

namespace BuildingBlocks.CQRS
{
    //In this context, Unit is a special type provided by the MediatR library. It is similar to void but is used where a type is required.
    public interface ICommand : ICommand<Unit>
    {
    }

    //IRequest<TResponse> is from MediatR. It marks something as a request expecting a response of type TResponse.
    public interface ICommand<out TResponse> : IRequest<TResponse>
    {
    }
}
