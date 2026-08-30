namespace TheAdamsParadigm.Api.Models
{
    public class YocoPaymentMetadata
    {
        public string CheckoutId { get; set; } = string.Empty;

        public string Fingerprint { get; set; } = string.Empty;

        public string ProductType { get; set; } = string.Empty;

        public string? OrderId { get; set; }
    }
}
