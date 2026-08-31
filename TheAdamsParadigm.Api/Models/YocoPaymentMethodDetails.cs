namespace TheAdamsParadigm.Api.Models
{
    public class YocoPaymentMethodDetails
    {
        public YocoCardDetails? Card { get; set; }

        public string Type { get; set; } = string.Empty;
    }
}
