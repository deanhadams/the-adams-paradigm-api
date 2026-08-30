namespace TheAdamsParadigm.Api.Models
{
    public class CreateCheckoutRequest
    {
        public string OrderId { get; set; } = string.Empty;

        public decimal Amount { get; set; }
    }
}
