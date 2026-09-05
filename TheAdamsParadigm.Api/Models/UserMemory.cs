namespace TheAdamsParadigm.Api.Models
{
    public class UserMemory
    {
        public int Id { get; set; }

        public string ChatUserId { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string Text { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
