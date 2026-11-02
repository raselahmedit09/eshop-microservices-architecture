namespace BuildingBlocks.Messaging.Events
{
    public record BasketCheckoutEvent : IntegrationEvent
    {
        public string UserName { get; set; } = default!;
        public Guid CustomerId { get; set; } = default!;
        public decimal TotalPrice { get; set; } = default!;

        // Shipping and BillingAddress
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public string EmailAddress { get; set; } = default!;
        public string AddressLine { get; set; } = default!;
        public string Country { get; set; } = default!;
        public string State { get; set; } = default!;
        public string ZipCode { get; set; } = default!;

        // Payment. CardNumber is masked (last 4 digits only) and the CVV is deliberately
        // not part of the event, so full card data never goes through the message broker.
        public string CardName { get; set; } = default!;
        public string CardNumber { get; set; } = default!;
        public string Expiration { get; set; } = default!;
        public int PaymentMethod { get; set; } = default!;

        // Items of the checked-out basket, so the order is created with what the user actually bought
        public List<BasketCheckoutItem> Items { get; set; } = new();
    }

    public record BasketCheckoutItem(Guid ProductId, string ProductName, int Quantity, decimal Price);
}
