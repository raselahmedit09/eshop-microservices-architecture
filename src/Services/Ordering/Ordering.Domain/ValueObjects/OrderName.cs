using Ordering.Domain.Exceptions;

namespace Ordering.Domain.ValueObjects
{
    public record OrderName
    {
        private const int DefaultLength = 3;
        public string Value { get; }

        private OrderName(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length < DefaultLength)
            {
                throw new DomainException($"OrderName must be at least {DefaultLength} characters long.");
            }

            Value = value;
        }

        public static OrderName Of(string value) => new OrderName(value);
    }
}
