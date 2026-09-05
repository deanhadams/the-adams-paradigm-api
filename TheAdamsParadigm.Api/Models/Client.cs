using System.Text.Json.Serialization;

namespace TheAdamsParadigm.Api.Models
{
    public class Client
    {
        public int ClientId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Website { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string ICloudEmail { get; set; } = string.Empty;

        // Stored encrypted at rest via ClientCredentialProtector (ASP.NET Core Data
        // Protection) — this is ciphertext, never plaintext. Only ClientCredentialProtector
        // should read or write this property. JsonIgnore is a backstop so it can never leak
        // out through an API response even if a future endpoint carelessly returns a Client.
        [JsonIgnore]
        public string ICloudPassword { get; set; } = string.Empty;

        public string ClientApiKey { get; set; } = string.Empty;
    }
}
