namespace TheAdamsParadigm.Api.Models.Calendar
{
    public class UpdateICloudCalendarEventRequest
    {
        public string Summary { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public DateTime Start { get; set; }

        public DateTime End { get; set; }
    }
}
