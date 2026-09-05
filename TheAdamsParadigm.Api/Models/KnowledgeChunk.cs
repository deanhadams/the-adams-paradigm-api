using Pgvector;

namespace TheAdamsParadigm.Api.Models
{
    public class KnowledgeChunk
    {
        public int Id { get; set; }

        public string Section { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        // Dimension must match VoyageEmbeddingService.EmbeddingDimension (voyage-3.5, 1024).
        public Vector Embedding { get; set; } = new(new float[1024]);

        public DateTime CreatedAt { get; set; }
    }
}
