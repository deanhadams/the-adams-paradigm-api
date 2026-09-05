namespace TheAdamsParadigm.Api.Models.Calendar
{
    public class BookingAvailabilityRequest
    {
        public DateTime Date { get; set; }

        public int DurationMinutes { get; set; }

        public int SlotIntervalMinutes { get; set; } = 30;

        public TimeSpan BusinessStart { get; set; } =
            new TimeSpan(9, 0, 0);

        public TimeSpan BusinessEnd { get; set; } =
            new TimeSpan(17, 0, 0);
    }
}
