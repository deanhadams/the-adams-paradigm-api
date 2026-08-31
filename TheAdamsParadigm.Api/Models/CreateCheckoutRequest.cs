namespace TheAdamsParadigm.Api.Models
{
    public class CreateCheckoutRequest
    {
        public string OrderId { get; set; } = string.Empty;

        public int? ServiceId { get; set; }

        public decimal Amount { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Surname { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
    }
}
