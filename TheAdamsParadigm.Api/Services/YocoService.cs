using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using TheAdamsParadigm.Api.Configuration;

namespace TheAdamsParadigm.Api.Services;

public class YocoService
{
    private readonly HttpClient _httpClient;
    private readonly YocoSettings _settings;

    public YocoService(HttpClient httpClient, IOptions<YocoSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;

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

        request.Content = JsonContent.Create(new
        {
            amount = amountInCents,
            currency = "ZAR",
            metadata = new
            {
                orderId = orderId
            }
        });

        var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Yoco returned {(int)response.StatusCode}: {responseBody}");
        }

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
            throw new HttpRequestException(
                $"Yoco webhook registration failed. " +
                $"Status: {(int)response.StatusCode}. " +
                $"Response: {responseBody}");
        }

        return responseBody;
    }
}
