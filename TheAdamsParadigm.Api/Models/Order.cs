namespace TheAdamsParadigm.Api.Models
{
    public class Order
    {
        public string OrderId { get; set; } = string.Empty;

        public int? ServiceId { get; set; }

        public decimal Amount { get; set; }

        public string Currency { get; set; } = "ZAR";

        public string Status { get; set; } = "Pending";

        public string? CheckoutId { get; set; }

        public string? PaymentId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Surname { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime? PaidAt { get; set; }

        // Navigation property
        public Service? Service { get; set; }
    }
}
