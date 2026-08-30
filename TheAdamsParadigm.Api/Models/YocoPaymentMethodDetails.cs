namespace TheAdamsParadigm.Api.Models
{
    public class YocoPaymentMethodDetails
    {
        public string? CardNumber { get; set; }

        public string? CardBrand { get; set; }

        public string? Holder { get; set; }

        public string? ExpiryDate { get; set; }
    }
}
