using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using TheAdamsParadigm.Api.Configuration;
using TheAdamsParadigm.Api.Data;
using TheAdamsParadigm.Api.Models;

namespace TheAdamsParadigm.Api.Services;

public class KnowledgeSearchService
{
    private const int MaxResults = 5;

    private const string NoInfoFoundContext = """
        No specific knowledge-base section matched
        the visitor's question.

        Do not invent information. If the question
        cannot be answered accurately, explain that
        the information is not currently available.
        """;

    private static KnowledgeSearchResult NoInfoFoundResult(string intent) => new()
    {
        Intent = intent,
        Sections = [],
        Context = NoInfoFoundContext,
    };

    private readonly ApplicationDbContext _dbContext;
    private readonly VoyageEmbeddingService _embeddingService;
    private readonly KnowledgeSearchSettings _settings;
    private readonly ILogger<KnowledgeSearchService> _logger;

    public KnowledgeSearchService(
        ApplicationDbContext dbContext,
        VoyageEmbeddingService embeddingService,
        IOptions<KnowledgeSearchSettings> settings,
        ILogger<KnowledgeSearchService> logger)
    {
        _dbContext = dbContext;
        _embeddingService = embeddingService;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<KnowledgeSearchResult> SearchAsync(string query)
    {
        var normalizedQuery = query.ToLowerInvariant();

        // Intent is a separate signal from retrieval (drives ProjectDiscoveryService
        // and is surfaced directly in the system prompt) — unchanged from before.
        var intent = DetectIntent(normalizedQuery);

        List<KnowledgeChunk>? topChunks;

        try
        {
            topChunks = await FindNearestChunksAsync(query);
        }
        catch (Exception ex)
        {
            // The Voyage API being down, rate-limited, or erroring should degrade the
            // chat to "no info found" rather than crash the request — the visitor
            // still gets a response, just without knowledge-base grounding this turn.
            _logger.LogError(ex, "Knowledge search failed; falling back to no-info-found");
            return NoInfoFoundResult(intent);
        }

        if (topChunks == null || topChunks.Count == 0)
        {
            return NoInfoFoundResult(intent);
        }

        return new KnowledgeSearchResult
        {
            Intent = intent,
            Sections = topChunks.Select(x => x.Section).ToList(),
            Context = string.Join(
                "\n\n----------------------------\n\n",
                topChunks.Select(x => x.Content))
        };
    }

    private async Task<List<KnowledgeChunk>?> FindNearestChunksAsync(string query)
    {
        var queryEmbedding = await _embeddingService.EmbedQueryAsync(query);
        var queryVector = new Vector(queryEmbedding);

        var candidates = await _dbContext.KnowledgeChunks
            .OrderBy(c => c.Embedding.CosineDistance(queryVector))
            .Take(MaxResults)
            .Select(c => new
            {
                Chunk = c,
                Distance = c.Embedding.CosineDistance(queryVector)
            })
            .ToListAsync();

        return candidates
            .Where(c => (1 - c.Distance) >= _settings.MinCosineSimilarity)
            .Select(c => c.Chunk)
            .ToList();
    }

    // =============================================================
    // Intent detection
    // =============================================================

    private static string DetectIntent(string query)
    {
        if (ContainsAny(
                query,
                [
                    "price",
                    "pricing",
                    "cost",
                    "how much",
                    "rate",
                    "hourly",
                    "expensive",
                    "budget"
                ]))
        {
            return "Pricing";
        }

        if (ContainsAny(
                query,
                [
                    "book",
                    "booking",
                    "appointment",
                    "schedule",
                    "availability",
                    "calendar"
                ]))
        {
            return "Booking";
        }

        if (ContainsAny(
                query,
                [
                    "contact",
                    "email",
                    "hire",
                    "reach",
                    "get in touch",
                    "quote"
                ]))
        {
            return "Contact";
        }

        if (ContainsAny(
                query,
                [
                    "project",
                    "portfolio",
                    "built",
                    "created",
                    "case study"
                ]))
        {
            return "Projects";
        }

        if (ContainsAny(
                query,
                [
                    "technology",
                    "technologies",
                    "tech stack",
                    "react",
                    "typescript",
                    "javascript",
                    "c#",
                    ".net",
                    "asp.net",
                    "postgres",
                    "database",
                    "signalr",
                    "ai"
                ]))
        {
            return "Technologies";
        }

        if (ContainsAny(
                query,
                [
                    "process",
                    "workflow",
                    "how do you build",
                    "how do you develop",
                    "development process"
                ]))
        {
            return "Process";
        }

        if (ContainsAny(
                query,
                [
                    "who is dean",
                    "who are you",
                    "about dean",
                    "about the company",
                    "about the business"
                ]))
        {
            return "About";
        }

        if (ContainsAny(
                query,
                [
                    "service",
                    "services",
                    "website",
                    "web app",
                    "application",
                    "software",
                    "api",
                    "build",
                    "develop"
                ]))
        {
            return "Services";
        }

        return "General";
    }

    // =============================================================
    // Keyword helper
    // =============================================================

    private static bool ContainsAny(
        string query,
        IEnumerable<string> keywords)
    {
        return keywords.Any(keyword =>
            query.Contains(
                keyword.ToLowerInvariant()));
    }
}
