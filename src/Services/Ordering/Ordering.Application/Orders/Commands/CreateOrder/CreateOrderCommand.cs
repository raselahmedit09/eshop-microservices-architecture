using Ordering.Application.Dtos;

namespace Ordering.Application.Orders.Commands.CreateOrder
{
    // UseOrderIdFromDto keeps Order.Id instead of generating one, so a consumer can create the order idempotently
    public record CreateOrderCommand(OrderDto Order) : ICommand<CreateOrderResult>
    {
        public bool UseOrderIdFromDto { get; init; }
    }

    public record CreateOrderResult(Guid Id);

    public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
    {
        public CreateOrderCommandValidator()
        {
            RuleFor(x => x.Order.OrderName).NotEmpty().WithMessage("Name is required");
            RuleFor(x => x.Order.CustomerId).NotNull().WithMessage("CustomerId is required");
            RuleFor(x => x.Order.OrderItems).NotEmpty().WithMessage("OrderItems should not be empty");
        }
    }
}
