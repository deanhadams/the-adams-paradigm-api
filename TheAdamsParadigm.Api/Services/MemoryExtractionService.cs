using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TheAdamsParadigm.Api.Configuration;
using TheAdamsParadigm.Api.Data;
using TheAdamsParadigm.Api.Models;

namespace TheAdamsParadigm.Api.Services;

public class MemoryExtractionService
{
    private static readonly string[] AllowedCategories = ["profile", "preferences", "context"];

    private readonly HttpClient _httpClient;
    private readonly AnthropicSettings _settings;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MemoryExtractionService> _logger;

    public MemoryExtractionService(
        HttpClient httpClient,
        IOptions<AnthropicSettings> settings,
        IServiceScopeFactory scopeFactory,
        ILogger<MemoryExtractionService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    // Fire-and-forget entry point: called without being awaited, so this must
    // never throw. The caller's request scope (and its ApplicationDbContext)
    // may already be gone by the time this actually runs, so DB access below
    // opens its own scope rather than taking one via constructor injection.
    public async Task ExtractAndStoreAsync(string chatUserId, IReadOnlyList<ChatMessage> conversation)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(chatUserId) || conversation.Count == 0)
            {
                return;
            }

            var facts = await ExtractFactsAsync(conversation);
            if (facts.Count == 0)
            {
                return;
            }

            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var existing = await dbContext.UserMemories
                .Where(m => m.ChatUserId == chatUserId)
                .Select(m => m.Text)
                .ToListAsync();

            var now = DateTime.UtcNow;
            var toInsert = new List<UserMemory>();

            foreach (var fact in facts)
            {
                if (string.IsNullOrWhiteSpace(fact.Text) ||
                    !AllowedCategories.Contains(fact.Category, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (existing.Any(e => IsSimilar(e, fact.Text)) ||
                    toInsert.Any(e => IsSimilar(e.Text, fact.Text)))
                {
                    continue;
                }

                toInsert.Add(new UserMemory
                {
                    ChatUserId = chatUserId,
                    Category = fact.Category.ToLowerInvariant(),
                    Text = fact.Text.Trim(),
                    CreatedAt = now,
                    UpdatedAt = now,
                });
            }

            if (toInsert.Count == 0)
            {
                return;
            }

            dbContext.UserMemories.AddRange(toInsert);
            await dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Stored {Count} new memory fact(s) for chat user {ChatUserId}",
                toInsert.Count,
                chatUserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Memory extraction failed for chat user {ChatUserId}", chatUserId);
        }
    }

    private async Task<List<ExtractedFact>> ExtractFactsAsync(IReadOnlyList<ChatMessage> conversation)
    {
        const string systemPrompt = """
            You extract durable facts about a website visitor from a conversation transcript.

            Rules:
            - Only extract facts the visitor explicitly stated. Never infer, guess, or assume.
            - Never extract sensitive data: health, financial details, government IDs, religion,
              sexual orientation, or political views. Skip these entirely, even if mentioned.
            - Skip temporary or one-off details (e.g. "I'm busy today") that won't matter later.
            - Each fact must be categorized as exactly one of: "profile", "preferences", "context".
            - If there is nothing worth remembering, return an empty facts array.

            Respond with strict JSON only, no other text, matching exactly this shape:
            {"facts": [{"category": "profile", "text": "..."}]}
            """;

        var transcript = new StringBuilder();
        foreach (var message in conversation)
        {
            var speaker = message.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase) ? "Assistant" : "User";
            transcript.AppendLine($"{speaker}: {message.Content}");
        }

        var userPrompt = $"""
            Conversation transcript:

            {transcript}

            Extract durable facts about the user from this transcript, following the rules above.
            """;

        var request = new
        {
            model = "claude-sonnet-5",
            max_tokens = 500,
            // See the matching comment in ClaudeService: Sonnet 5 defaults to adaptive
            // thinking when this is omitted, which can consume the whole max_tokens
            // budget before any text is produced. No tool calls happen here either.
            thinking = new { type = "disabled" },
            system = systemPrompt,
            messages = new[]
            {
                new { role = "user", content = userPrompt },
            },
        };

        var json = JsonSerializer.Serialize(request);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "v1/messages")
        {
            Content = content,
        };
        httpRequest.Headers.Add("x-api-key", _settings.ApiKey);
        httpRequest.Headers.Add("anthropic-version", "2023-06-01");
        httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _httpClient.SendAsync(httpRequest);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Memory extraction Claude call failed: {StatusCode} - {Body}",
                response.StatusCode,
                responseBody);
            return [];
        }

        using var document = JsonDocument.Parse(responseBody);
        if (!document.RootElement.TryGetProperty("content", out var contentElement) ||
            contentElement.ValueKind != JsonValueKind.Array ||
            contentElement.GetArrayLength() == 0)
        {
            return [];
        }

        var text = contentElement[0].TryGetProperty("text", out var textElement)
            ? textElement.GetString()
            : null;

        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<ExtractionResult>(
                StripMarkdownCodeFence(text),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return parsed?.Facts ?? [];
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Could not parse memory extraction JSON: {Text}", text);
            return [];
        }
    }

    // Claude is instructed to respond with strict JSON only, but sometimes wraps it
    // in a markdown code fence (```json ... ```) anyway. Strip that before parsing.
    private static string StripMarkdownCodeFence(string text)
    {
        var trimmed = text.Trim();

        if (!trimmed.StartsWith("```"))
        {
            return trimmed;
        }

        var firstNewline = trimmed.IndexOf('\n');
        if (firstNewline == -1)
        {
            return trimmed;
        }

        trimmed = trimmed[(firstNewline + 1)..];

        var closingFenceIndex = trimmed.LastIndexOf("```", StringComparison.Ordinal);
        if (closingFenceIndex != -1)
        {
            trimmed = trimmed[..closingFenceIndex];
        }

        return trimmed.Trim();
    }

    private static bool IsSimilar(string a, string b)
    {
        var normalizedA = Normalize(a);
        var normalizedB = Normalize(b);

        if (normalizedA.Length == 0 || normalizedB.Length == 0)
        {
            return false;
        }

        if (normalizedA == normalizedB)
        {
            return true;
        }

        if (normalizedA.Length > 12 && normalizedB.Contains(normalizedA))
        {
            return true;
        }

        if (normalizedB.Length > 12 && normalizedA.Contains(normalizedB))
        {
            return true;
        }

        return false;
    }

    private static string Normalize(string text) => text.Trim().ToLowerInvariant();

    private class ExtractionResult
    {
        [JsonPropertyName("facts")]
        public List<ExtractedFact> Facts { get; set; } = [];
    }

    private class ExtractedFact
    {
        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }
}
