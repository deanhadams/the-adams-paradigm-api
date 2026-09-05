namespace TheAdamsParadigm.Api.Services.CloudCalendarService;

// Thrown by ICloudCalendarService when a clientId doesn't resolve to an existing Client row.
public class ClientNotFoundException(int clientId)
    : Exception($"Client {clientId} was not found.")
{
    public int ClientId { get; } = clientId;
}

// Thrown by ICloudCalendarService when the Client exists but hasn't got iCloud
// credentials configured (ICloudEmail/ICloudPassword empty) — a distinct, more specific
// case than "not found" so the controller can return 400 rather than 404 for it.
public class ClientCloudCredentialsMissingException(int clientId)
    : Exception($"Client {clientId} does not have iCloud credentials configured.")
{
    public int ClientId { get; } = clientId;
}
