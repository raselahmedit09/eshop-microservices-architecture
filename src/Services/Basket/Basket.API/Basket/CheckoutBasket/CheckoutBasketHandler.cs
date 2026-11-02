using Basket.API.Data;
using Basket.API.Dtos;
using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging.Events;
using FluentValidation;
using Mapster;
using MassTransit;

namespace Basket.API.Basket.CheckoutBasket
{
    public record CheckoutBasketCommand(BasketCheckoutDto BasketCheckoutDto) : ICommand<CheckoutBasketResult>;
    public record CheckoutBasketResult(bool IsSuccess);

    public class CheckoutBasketCommandValidator
    : AbstractValidator<CheckoutBasketCommand>
    {
        public CheckoutBasketCommandValidator()
        {
            RuleFor(x => x.BasketCheckoutDto).NotNull().WithMessage("BasketCheckoutDto can't be null");
            RuleFor(x => x.BasketCheckoutDto.UserName).NotEmpty().WithMessage("UserName is required");
        }
    }

    public class CheckoutBasketCommandHandler(IBasketRepository repository, IPublishEndpoint publishEndpoint) : ICommandHandler<CheckoutBasketCommand, CheckoutBasketResult>
    {
        public async Task<CheckoutBasketResult> Handle(CheckoutBasketCommand command, CancellationToken cancellationToken)
        {
            // get existing basket with total price
            // Set totalprice and items on basketcheckout event message
            // send basket checkout event to rabbitmq using masstransit
            // delete the basket

            // Throws BasketNotFoundException (404) when the user has no basket
            var basket = await repository.GetBasket(command.BasketCheckoutDto.UserName, cancellationToken);

            var eventMessage = command.BasketCheckoutDto.Adapt<BasketCheckoutEvent>();
            eventMessage.TotalPrice = basket.TotalPrice;
            eventMessage.Items = basket.Items
                .Select(i => new BasketCheckoutItem(i.ProductId, i.ProductName, i.Quantity, i.DiscountedPrice))
                .ToList();
            // Only the last 4 digits leave this service; the CVV is never forwarded
            eventMessage.CardNumber = MaskCardNumber(command.BasketCheckoutDto.CardNumber);

            await publishEndpoint.Publish(eventMessage, cancellationToken);

            await repository.DeleteBasket(command.BasketCheckoutDto.UserName, cancellationToken);

            return new CheckoutBasketResult(true);
        }

        private static string MaskCardNumber(string cardNumber)
        {
            var digits = new string(cardNumber.Where(char.IsDigit).ToArray());
            var last4 = digits.Length >= 4 ? digits[^4..] : digits;
            return $"**** **** **** {last4}";
        }
    }
}
