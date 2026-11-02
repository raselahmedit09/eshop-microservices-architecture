namespace Basket.API.Models
{
    public class ShoppingCartItem
    {
        public int Quantity { get; set; } = default!;
        public string Color { get; set; } = default!;
        // Original unit price; never changed by the server
        public decimal Price { get; set; } = default!;
        // Discount per unit from the Discount service; recalculated (not accumulated) on every store
        public decimal Discount { get; set; } = default!;
        public decimal DiscountedPrice => Price - Discount;
        public Guid ProductId { get; set; } = default!;
        public string ProductName { get; set; } = default!;
    }
}
