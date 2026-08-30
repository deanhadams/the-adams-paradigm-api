namespace TheAdamsParadigm.Api.Models
{
    public class YocoPaymentPayload
    {
        public int Amount { get; set; }

        public DateTime CreatedDate { get; set; }

        public string Currency { get; set; } = string.Empty;

        public string Id { get; set; } = string.Empty;

        public YocoPaymentMetadata Metadata { get; set; } = new();

        public string Mode { get; set; } = string.Empty;

        public YocoPaymentMethodDetails PaymentMethodDetails { get; set; } = new();

        public string Status { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;
    }
}
