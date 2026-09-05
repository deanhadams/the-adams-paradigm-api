namespace TheAdamsParadigm.Api.Models.Calendar
{
    public class ICloudAvailabilityResponse
    {
        public bool Available { get; set; }

        public DateTime Start { get; set; }

        public DateTime End { get; set; }

        public List<ICloudCalendarEvent> Conflicts { get; set; } = [];
    }
}
