namespace TheAdamsParadigm.Api.Configuration;

// The ClientApiKey identifying this site's own iCloud calendar client row, so the
// public booking flow (available slots + checkout) never needs the browser to know it.
public class BookingSettings
{
    public string ClientApiKey { get; set; } = string.Empty;

    public int DefaultDurationMinutes { get; set; } = 60;

    public int SlotIntervalMinutes { get; set; } = 30;
}
