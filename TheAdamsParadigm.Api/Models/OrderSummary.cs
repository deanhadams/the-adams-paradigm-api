namespace TheAdamsParadigm.Api.Models
{
    public class OrderSummary
    {
        public string OrderNumber { get; set; } = string.Empty;

        public string? PaymentLink { get; set; }

        public string PaymentStatus { get; set; } = string.Empty;
    }
}
