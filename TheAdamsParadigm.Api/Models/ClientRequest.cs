namespace TheAdamsParadigm.Api.Models
{
    public class ClientRequest
    {
        public string Name { get; set; } = string.Empty;

        public string Website { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string ICloudEmail { get; set; } = string.Empty;

        // Name of the iCloud calendar to book against (e.g. "Bookings"). Defaults to
        // "Bookings" if left blank.
        public string ICloudCalendar { get; set; } = "Bookings";

        // Plaintext on the wire — the controller encrypts this via ClientCredentialProtector
        // before it's ever written to the database. Null/omitted on update means "leave the
        // stored password unchanged."
        public string? ICloudPassword { get; set; }
    }
}
