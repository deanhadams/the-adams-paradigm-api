namespace TheAdamsParadigm.Api.Configuration;

// Non-secret scheduling defaults for the public booking flow. Which client's calendar
// to book against (ClientApiKey) comes from the caller on each request instead of
// living here — see BookingsController/PaymentsController.
public class BookingSettings
{
    public int DefaultDurationMinutes { get; set; } = 60;

    public int SlotIntervalMinutes { get; set; } = 30;
}
