using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TheAdamsParadigm.Api.Configuration;

namespace TheAdamsParadigm.Api.Services;

public class YocoService
{
    private readonly HttpClient _httpClient;
    private readonly YocoSettings _settings;
    private readonly ILogger<YocoService> _logger;

    public YocoService(HttpClient httpClient, IOptions<YocoSettings> settings, ILogger<YocoService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _settings.SecretKey);
    }

    public async Task<string> CreateCheckoutAsync(string orderId, decimal amount)
    {
        if (string.IsNullOrWhiteSpace(orderId))
        {
            throw new ArgumentException("Order ID is required.");
        }

        if (amount <= 0)
        {
            throw new ArgumentException("Amount must be greater than zero.");
        }

        var amountInCents = (int)Math.Round(
            amount * 100,
            MidpointRounding.AwayFromZero);

        var idempotencyKey = Guid.NewGuid().ToString();

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "api/checkouts");

        request.Headers.Add("Idempotency-Key", idempotencyKey);

        var outgoingPayload = new
        {
            amount = amountInCents,
            currency = "ZAR",
            metadata = new
            {
                orderId = orderId
            }
        };

        request.Content = JsonContent.Create(outgoingPayload);

        var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Yoco checkout creation failed. Status: {StatusCode}. Request payload: {RequestPayload}. Response body: {ResponseBody}",
                (int)response.StatusCode,
                JsonSerializer.Serialize(outgoingPayload),
                responseBody);

            throw new HttpRequestException(
                $"Yoco returned {(int)response.StatusCode}: {responseBody}");
        }

        _logger.LogInformation(
            "Yoco checkout created successfully. Status: {StatusCode}. Order: {OrderId}",
            (int)response.StatusCode,
            orderId);

        return responseBody;
    }

    public async Task<string> RegisterWebhookAsync(string webhookUrl)
    {
        if (string.IsNullOrWhiteSpace(webhookUrl))
        {
            throw new ArgumentException("Webhook URL is required.");
        }

        var request = new
        {
            name = "Local Yoco Test Webhook",
            url = webhookUrl
        };

        var response = await _httpClient.PostAsJsonAsync(
            "api/webhooks",
            request);

        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Yoco webhook registration failed. Status: {StatusCode}. Request payload: {RequestPayload}. Response body: {ResponseBody}",
                (int)response.StatusCode,
                JsonSerializer.Serialize(request),
                responseBody);

            throw new HttpRequestException(
                $"Yoco webhook registration failed. " +
                $"Status: {(int)response.StatusCode}. " +
                $"Response: {responseBody}");
        }

        _logger.LogInformation(
            "Yoco webhook registered successfully. Status: {StatusCode}.",
            (int)response.StatusCode);

        return responseBody;
    }
}
