namespace TheAdamsParadigm.Api.Models
{
    public class YocoCheckoutResponse
    {
        public string Id { get; set; } = string.Empty;

        public string RedirectUrl { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public int Amount { get; set; }

        public string Currency { get; set; } = string.Empty;

        public string? MerchantId { get; set; }

        public string? ProcessingMode { get; set; }

        public Dictionary<string, string>? Metadata { get; set; }
    }
}
