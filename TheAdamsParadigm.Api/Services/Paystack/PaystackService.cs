using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Options;
using TheAdamsParadigm.Api.Configuration;
using TheAdamsParadigm.Api.Models.Paystack;

namespace TheAdamsParadigm.Api.Services.Paystack;

public interface IPaystackService
{
    Task<PaystackInitializeData> InitializeTransactionAsync(
        string orderId,
        string reference,
        string email,
        decimal amount,
        CancellationToken cancellationToken = default);

    Task<PaystackTransactionData> VerifyTransactionAsync(
        string reference,
        string? orderId = null,
        CancellationToken cancellationToken = default);

    bool IsValidWebhookSignature(byte[] rawBody, string? signatureHeader);
}

public class PaystackService : IPaystackService
{
    public const string Currency = "ZAR";

    private readonly HttpClient _httpClient;
    private readonly PaystackSettings _settings;
    private readonly ILogger<PaystackService> _logger;

    public PaystackService(HttpClient httpClient, IOptions<PaystackSettings> settings, ILogger<PaystackService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    // Paystack amounts are in the smallest currency unit (cents for ZAR). Same rounding
    // as YocoService so a given order total converts identically under both providers.
    public static long ToSmallestUnit(decimal amount)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Amount must be greater than zero.");
        }

        var cents = decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero);

        return checked((long)cents);
    }

    public async Task<PaystackInitializeData> InitializeTransactionAsync(
        string orderId,
        string reference,
        string email,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        EnsureSecretKey();

        if (string.IsNullOrWhiteSpace(_settings.CallbackUrl))
        {
            throw new PaystackConfigurationException(
                "Paystack callback URL is not configured. Set the Paystack__CallbackUrl environment variable.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required for Paystack payments.");
        }

        var amountInCents = ToSmallestUnit(amount);

        var payload = new
        {
            email,
            amount = amountInCents,
            currency = Currency,
            reference,
            callback_url = _settings.CallbackUrl,
            metadata = new
            {
                order_id = orderId,
                order_number = orderId
            }
        };

        using var request = CreateRequest(HttpMethod.Post, "transaction/initialize");
        request.Content = JsonContent.Create(payload);

        var envelope = await SendAsync<PaystackInitializeData>(
            request, "initialize transaction", orderId, reference, cancellationToken);

        _logger.LogInformation(
            "Paystack transaction initialized. Order: {OrderId}. Reference: {Reference}. Amount: {AmountInCents} {Currency}",
            orderId,
            envelope.Reference,
            amountInCents,
            Currency);

        return envelope;
    }

    public async Task<PaystackTransactionData> VerifyTransactionAsync(
        string reference,
        string? orderId = null,
        CancellationToken cancellationToken = default)
    {
        EnsureSecretKey();

        if (string.IsNullOrWhiteSpace(reference))
        {
            throw new ArgumentException("Reference is required.");
        }

        using var request = CreateRequest(
            HttpMethod.Get,
            $"transaction/verify/{Uri.EscapeDataString(reference)}");

        var data = await SendAsync<PaystackTransactionData>(
            request, "verify transaction", orderId, reference, cancellationToken);

        _logger.LogInformation(
            "Paystack transaction verified. Order: {OrderId}. Reference: {Reference}. Paystack status: {PaystackStatus}. Amount: {AmountInCents} {Currency}. Gateway response: {GatewayResponse}",
            orderId,
            reference,
            data.Status,
            data.Amount,
            data.Currency,
            data.GatewayResponse);

        return data;
    }

    // x-paystack-signature is the hex HMAC-SHA512 of the raw request body, keyed with the secret key.
    public bool IsValidWebhookSignature(byte[] rawBody, string? signatureHeader)
    {
        EnsureSecretKey();

        if (string.IsNullOrWhiteSpace(signatureHeader))
        {
            return false;
        }

        byte[] provided;
        try
        {
            provided = Convert.FromHexString(signatureHeader.Trim());
        }
        catch (FormatException)
        {
            return false;
        }

        var expected = ComputeSignature(rawBody, _settings.SecretKey);

        return CryptographicOperations.FixedTimeEquals(expected, provided);
    }

    public static byte[] ComputeSignature(byte[] rawBody, string secretKey) =>
        HMACSHA512.HashData(System.Text.Encoding.UTF8.GetBytes(secretKey), rawBody);

    private HttpRequestMessage CreateRequest(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.SecretKey);
        return request;
    }

    private async Task<T> SendAsync<T>(
        HttpRequestMessage request,
        string operation,
        string? orderId,
        string reference,
        CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex,
                "Paystack {Operation} could not reach Paystack. Order: {OrderId}. Reference: {Reference}",
                operation, orderId, reference);
            throw new PaystackApiException($"Could not reach Paystack to {operation}: {ex.Message}", null, null, ex);
        }

        using (response)
        {
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Paystack {Operation} failed. Status: {StatusCode}. Order: {OrderId}. Reference: {Reference}. Response body: {ResponseBody}",
                    operation, (int)response.StatusCode, orderId, reference, responseBody);

                throw new PaystackApiException(
                    $"Paystack {operation} returned {(int)response.StatusCode}: {ExtractMessage(responseBody)}",
                    (int)response.StatusCode,
                    responseBody);
            }

            PaystackEnvelope<T>? envelope;
            try
            {
                envelope = JsonSerializer.Deserialize<PaystackEnvelope<T>>(responseBody);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex,
                    "Paystack {Operation} returned unparseable JSON. Status: {StatusCode}. Order: {OrderId}. Reference: {Reference}. Response body: {ResponseBody}",
                    operation, (int)response.StatusCode, orderId, reference, responseBody);

                throw new PaystackApiException(
                    $"Paystack {operation} returned an invalid response.",
                    (int)response.StatusCode,
                    responseBody,
                    ex);
            }

            if (envelope is not { Status: true } || envelope.Data is null)
            {
                _logger.LogError(
                    "Paystack {Operation} returned status=false or no data. Status: {StatusCode}. Order: {OrderId}. Reference: {Reference}. Response body: {ResponseBody}",
                    operation, (int)response.StatusCode, orderId, reference, responseBody);

                throw new PaystackApiException(
                    $"Paystack {operation} was rejected: {envelope?.Message ?? "no message"}",
                    (int)response.StatusCode,
                    responseBody);
            }

            return envelope.Data;
        }
    }

    private static string ExtractMessage(string responseBody)
    {
        try
        {
            using var document = JsonDocument.Parse(responseBody);
            if (document.RootElement.TryGetProperty("message", out var message) &&
                message.ValueKind == JsonValueKind.String)
            {
                return message.GetString() ?? responseBody;
            }
        }
        catch (JsonException)
        {
        }

        return responseBody;
    }

    private void EnsureSecretKey()
    {
        if (string.IsNullOrWhiteSpace(_settings.SecretKey))
        {
            throw new PaystackConfigurationException(
                "Paystack secret key is not configured. Set the Paystack__SecretKey environment variable.");
        }
    }
}

public class PaystackConfigurationException : InvalidOperationException
{
    public PaystackConfigurationException(string message) : base(message)
    {
    }
}

public class PaystackApiException : Exception
{
    public int? StatusCode { get; }

    public string? ResponseBody { get; }

    public PaystackApiException(string message, int? statusCode, string? responseBody, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}
