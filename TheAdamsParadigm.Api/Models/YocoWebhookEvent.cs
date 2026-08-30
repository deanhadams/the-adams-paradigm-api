namespace TheAdamsParadigm.Api.Models
{
    public class YocoWebhookEvent
    {
        public DateTime CreatedDate { get; set; }

        public string Id { get; set; } = string.Empty;

        public YocoPaymentPayload Payload { get; set; } = new();

        public string Type { get; set; } = string.Empty;
    }
}
