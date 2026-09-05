using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using TheAdamsParadigm.Api.Configuration;

namespace TheAdamsParadigm.Api.Services;

public class VoyageEmbeddingService
{
    public const int EmbeddingDimension = 1024;

    private const string Model = "voyage-3.5";

    private readonly HttpClient _httpClient;
    private readonly VoyageSettings _settings;

    public VoyageEmbeddingService(HttpClient httpClient, IOptions<VoyageSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
    }

    // Voyage recommends input_type "document" when indexing a corpus and "query" when
    // embedding a search prompt — it prepends a task-specific prefix internally, which
    // improves retrieval quality over embedding both the same way.
    public async Task<float[]> EmbedQueryAsync(string text)
    {
        var results = await EmbedAsync([text], "query");
        return results[0];
    }

    // Batches the whole corpus into a single request (Voyage accepts up to 1000 inputs
    // per call) rather than one request per chunk — besides being the efficient way to
    // do it, low/free-tier Voyage accounts are rate-limited to as little as 3 requests
    // per minute, which one-per-chunk blows through immediately for any real knowledge base.
    public Task<List<float[]>> EmbedDocumentsAsync(IReadOnlyList<string> texts) =>
        EmbedAsync(texts, "document");

    private async Task<List<float[]>> EmbedAsync(IReadOnlyList<string> texts, string inputType)
    {
        var requestBody = new
        {
            input = texts,
            model = Model,
            input_type = inputType,
            output_dimension = EmbeddingDimension,
        };

        var json = JsonSerializer.Serialize(requestBody);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "v1/embeddings")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
        httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _settings.ApiKey);

        var response = await _httpClient.SendAsync(httpRequest);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Voyage AI embeddings request failed: {response.StatusCode} - {responseBody}");
        }

        var parsed = JsonSerializer.Deserialize<VoyageEmbeddingResponse>(
            responseBody,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        var data = parsed?.Data;

        if (data == null || data.Count != texts.Count)
        {
            throw new InvalidOperationException(
                $"Voyage AI returned {data?.Count ?? 0} embedding(s) for {texts.Count} input(s). Raw response: {responseBody}");
        }

        // The API documents results as ordered by "index", but sort explicitly rather
        // than trust response ordering implicitly.
        return data
            .OrderBy(d => d.Index)
            .Select(d => d.Embedding ?? throw new InvalidOperationException(
                $"Voyage AI returned a null embedding. Raw response: {responseBody}"))
            .ToList();
    }

    private class VoyageEmbeddingResponse
    {
        [JsonPropertyName("data")]
        public List<VoyageEmbeddingData>? Data { get; set; }
    }

    private class VoyageEmbeddingData
    {
        [JsonPropertyName("embedding")]
        public float[]? Embedding { get; set; }

        [JsonPropertyName("index")]
        public int Index { get; set; }
    }
}
