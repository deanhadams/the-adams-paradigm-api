namespace TheAdamsParadigm.Api.Models
{
    public class ClientResponse
    {
        public int ClientId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Website { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string ICloudEmail { get; set; } = string.Empty;

        public string ClientApiKey { get; set; } = string.Empty;

        // ICloudPassword is deliberately never included here, even encrypted —
        // there's no legitimate reason for an API response to carry it at all.
    }
}
