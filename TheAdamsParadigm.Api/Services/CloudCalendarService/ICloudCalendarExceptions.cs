namespace TheAdamsParadigm.Api.Services.CloudCalendarService;

// Thrown by ICloudCalendarService when a ClientApiKey doesn't resolve to an existing
// Client row.
public class ClientNotFoundException(string clientApiKey)
    : Exception($"No client was found for API key {clientApiKey}.")
{
    public string ClientApiKey { get; } = clientApiKey;
}

// Thrown by ICloudCalendarService when the Client exists but hasn't got iCloud
// credentials configured (ICloudEmail/ICloudPassword empty) — a distinct, more specific
// case than "not found" so the controller can return 400 rather than 404 for it.
public class ClientCloudCredentialsMissingException(string clientApiKey)
    : Exception($"Client with API key {clientApiKey} does not have iCloud credentials configured.")
{
    public string ClientApiKey { get; } = clientApiKey;
}
