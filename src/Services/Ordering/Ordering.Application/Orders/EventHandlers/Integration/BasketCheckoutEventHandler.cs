using BuildingBlocks.Messaging.Events;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Ordering.Application.Data;
using Ordering.Application.Dtos;
using Ordering.Application.Orders.Commands.CreateOrder;
using Ordering.Domain.ValueObjects;

namespace Ordering.Application.Orders.EventHandlers.Integration
{
    public class BasketCheckoutEventHandler(ISender sender, IApplicationDbContext dbContext, ILogger<BasketCheckoutEventHandler> logger) : IConsumer<BasketCheckoutEvent>
    {
        public async Task Consume(ConsumeContext<BasketCheckoutEvent> context)
        {
            // TODO: Create new order and start order fullfillment process
            logger.LogInformation("Integration Event handled: {IntegrationEvent}", context.Message.GetType().Name);

            // The order Id is the event Id, so the same checkout delivered twice creates only one order.
            // A concurrent duplicate that passes this check is still rejected by the primary key.
            var orderId = OrderId.Of(context.Message.Id);
            if (await dbContext.Orders.AnyAsync(o => o.Id == orderId, context.CancellationToken))
            {
                logger.LogWarning("Order {OrderId} already exists, skipping duplicate {IntegrationEvent}", orderId.Value, context.Message.GetType().Name);
                return;
            }

            var command = MapToCreateOrderCommand(context.Message);
            await sender.Send(command, context.CancellationToken);
        }

        private CreateOrderCommand MapToCreateOrderCommand(BasketCheckoutEvent message)
        {
            // Create full order with incoming event data
            var addressDto = new AddressDto(message.FirstName, message.LastName, message.EmailAddress, message.AddressLine, message.Country, message.State, message.ZipCode);
            var paymentDto = new PaymentDto(message.CardName, message.CardNumber, message.Expiration, Cvv: null, message.PaymentMethod);
            var orderId = message.Id;

            var orderDto = new OrderDto(
                Id: orderId,
                CustomerId: message.CustomerId,
                OrderName: message.UserName,
                ShippingAddress: addressDto,
                BillingAddress: addressDto,
                Payment: paymentDto,
                Status: Ordering.Domain.Enums.OrderStatus.Pending,
                OrderItems: message.Items
                    .Select(i => new OrderItemDto(orderId, i.ProductId, i.Quantity, i.Price))
                    .ToList());

            return new CreateOrderCommand(orderDto) { UseOrderIdFromDto = true };
        }
    }
}
