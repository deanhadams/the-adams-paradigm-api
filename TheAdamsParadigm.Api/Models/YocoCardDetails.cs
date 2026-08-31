namespace TheAdamsParadigm.Api.Models
{
    public class YocoCardDetails
    {
        public int ExpiryMonth { get; set; }

        public int ExpiryYear { get; set; }

        public string MaskedCard { get; set; } = string.Empty;

        public string Scheme { get; set; } = string.Empty;
    }
}
