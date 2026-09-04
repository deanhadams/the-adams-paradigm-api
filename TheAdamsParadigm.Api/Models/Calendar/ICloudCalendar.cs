namespace TheAdamsParadigm.Api.Models.Calendar
{
    public class ICloudCalendar
    {
        public string Name { get; set; } = string.Empty;

        public string Url { get; set; } = string.Empty;

        public bool IsCalendar { get; set; }

        public List<string> SupportedComponents { get; set; } = [];
    }
}
