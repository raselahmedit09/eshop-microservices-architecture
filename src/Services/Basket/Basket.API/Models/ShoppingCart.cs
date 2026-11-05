namespace Basket.API.Models
{
    public class ShoppingCart
    {
        public string UserName { get; set; } = default!;
        public List<ShoppingCartItem> Items { get; set; } = new();
        public decimal TotalPrice => Items.Sum(x => x.DiscountedPrice * x.Quantity);

        // Identifies this version of the basket for checkout. It changes whenever the basket is stored,
        // so checking out the same basket again reuses it and Ordering can skip the duplicate order.
        public Guid CheckoutId { get; set; }

        public ShoppingCart(string userName)
        {
            UserName = userName;
        }

        //Required for Mapping
        public ShoppingCart()
        {
        }
    }
}
