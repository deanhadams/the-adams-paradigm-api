namespace TheAdamsParadigm.Api.Models
{
    public class UserMemorySummary
    {
        public string Category { get; set; } = string.Empty;

        public string Text { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}
