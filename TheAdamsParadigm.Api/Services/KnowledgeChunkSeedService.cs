using Microsoft.EntityFrameworkCore;
using Pgvector;
using TheAdamsParadigm.Api.Data;
using TheAdamsParadigm.Api.Models;

namespace TheAdamsParadigm.Api.Services;

public class KnowledgeChunkSeedService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly KnowledgeBaseService _knowledgeBaseService;
    private readonly KnowledgeChunkBuilder _chunkBuilder;
    private readonly VoyageEmbeddingService _embeddingService;
    private readonly ILogger<KnowledgeChunkSeedService> _logger;

    public KnowledgeChunkSeedService(
        ApplicationDbContext dbContext,
        KnowledgeBaseService knowledgeBaseService,
        KnowledgeChunkBuilder chunkBuilder,
        VoyageEmbeddingService embeddingService,
        ILogger<KnowledgeChunkSeedService> logger)
    {
        _dbContext = dbContext;
        _knowledgeBaseService = knowledgeBaseService;
        _chunkBuilder = chunkBuilder;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    // Re-embeds every section/project/FAQ chunk from the current knowledge-base.json and
    // replaces whatever is currently stored. Safe to re-run any time the knowledge base
    // changes — embeds everything first, and only clears the old rows once every new
    // embedding has succeeded, so a failed Voyage call never leaves the table empty.
    public async Task<int> ReseedAsync()
    {
        var knowledgeBase = _knowledgeBaseService.GetKnowledgeBase();
        var sections = _chunkBuilder.Build(knowledgeBase);

        List<float[]> embeddings;

        try
        {
            embeddings = await _embeddingService.EmbedDocumentsAsync(
                sections.Select(s => s.Content).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Knowledge chunk reseed failed while calling Voyage AI — existing chunks left untouched");
            throw;
        }

        var now = DateTime.UtcNow;

        var newChunks = sections
            .Zip(embeddings, (section, embedding) => new KnowledgeChunk
            {
                Section = section.Section,
                Content = section.Content,
                Embedding = new Vector(embedding),
                CreatedAt = now,
            })
            .ToList();

        _logger.LogInformation("Embedded {Count} knowledge chunk(s)", newChunks.Count);

        await _dbContext.KnowledgeChunks.ExecuteDeleteAsync();

        _dbContext.KnowledgeChunks.AddRange(newChunks);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Reseeded {Count} knowledge chunk(s)", newChunks.Count);

        return newChunks.Count;
    }
}
