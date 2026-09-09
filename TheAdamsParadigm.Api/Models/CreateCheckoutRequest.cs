namespace TheAdamsParadigm.Api.Models
{
    public class CreateCheckoutRequest
    {
        public int? ServiceId { get; set; }

        public decimal Amount { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Surname { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        // Optional — omitted while calendar booking is hidden in the UI. See
        // BookingForm.tsx's CALENDAR_BOOKING_ENABLED flag.
        public DateTime? BookingStart { get; set; }

        public int DurationMinutes { get; set; } = 60;

        // Supplied by the frontend (VITE_CLIENT_API_KEY) rather than known server-side —
        // the API deliberately doesn't hold its own copy of which client it's booking for.
        public string ClientApiKey { get; set; } = string.Empty;
    }
}
