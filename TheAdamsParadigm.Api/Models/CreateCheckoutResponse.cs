namespace TheAdamsParadigm.Api.Models
{
    public class CreateCheckoutResponse
    {
        public string OrderId { get; set; } = string.Empty;

        public string? CheckoutId { get; set; }

        public string? PaymentUrl { get; set; }

        public decimal Amount { get; set; }

        public string Currency { get; set; } = string.Empty;

        public string? YocoStatus { get; set; }
    }
}
