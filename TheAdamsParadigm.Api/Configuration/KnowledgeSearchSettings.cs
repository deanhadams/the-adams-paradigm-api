namespace TheAdamsParadigm.Api.Configuration
{
    public class KnowledgeSearchSettings
    {
        // pgvector CosineDistance is 1 - cosine_similarity (0 = identical direction,
        // 2 = opposite). A retrieved chunk must clear this similarity bar to be
        // treated as relevant; below it, the search falls back to "no info found."
        public double MinCosineSimilarity { get; set; } = 0.5;
    }
}
