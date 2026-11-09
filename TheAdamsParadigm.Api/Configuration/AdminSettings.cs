namespace TheAdamsParadigm.Api.Configuration;

// Bound from the "Admin" section (Admin__ApiKey on Railway). When ApiKey is empty every
// endpoint marked [RequireAdminKey] is disabled and returns 503.
public class AdminSettings
{
    public string ApiKey { get; set; } = string.Empty;
}
