namespace TheAdamsParadigm.Api.Models
{
    public class Order
    {
        public string OrderId { get; set; } = string.Empty;

        public int? ServiceId { get; set; }

        public decimal Amount { get; set; }

        public string Currency { get; set; } = "ZAR";

        public string Status { get; set; } = "Pending";

        public string? CheckoutId { get; set; }

        public string? PaymentId { get; set; }

        public string? PaymentLink { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Surname { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime? PaidAt { get; set; }

        public DateTime? BookingStart { get; set; }

        public DateTime? BookingEnd { get; set; }

        // UID of the iCloud calendar event created once payment succeeds.
        public string? CalendarEventUid { get; set; }

        // Which client's iCloud calendar this booking was made against — captured from
        // the frontend-supplied key at checkout time, since the async Yoco webhook that
        // later creates the calendar event has no request from the browser to read it from.
        public string? ClientApiKey { get; set; }

        // Navigation property
        public Service? Service { get; set; }
    }
}
