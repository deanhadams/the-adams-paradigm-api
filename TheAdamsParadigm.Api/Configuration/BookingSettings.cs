namespace TheAdamsParadigm.Api.Configuration;

// Non-secret scheduling defaults for the public booking flow. Which client's calendar
// to book against (ClientApiKey) comes from the caller on each request instead of
// living here — see BookingsController/PaymentsController.
public class BookingSettings
{
    public int DefaultDurationMinutes { get; set; } = 60;

    public int SlotIntervalMinutes { get; set; } = 30;

    // Feature switch for the public booking flow. Set to false to hide the booking
    // menu link and section on the frontend (e.g. while sorting out payment issues)
    // without touching any deployed code.
    public bool Enabled { get; set; } = true;
}
