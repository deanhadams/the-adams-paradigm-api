namespace TheAdamsParadigm.Api.Models
{
    public class ContactRequest
    {
        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string ProjectType { get; set; } = string.Empty;

        public string Budget { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public string? ContextLabel { get; set; }
    }
}
