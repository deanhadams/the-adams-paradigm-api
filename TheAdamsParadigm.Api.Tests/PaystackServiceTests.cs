using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TheAdamsParadigm.Api.Configuration;
using TheAdamsParadigm.Api.Services.Paystack;
using Xunit;

namespace TheAdamsParadigm.Api.Tests;

public class PaystackServiceTests
{
    private const string SecretKey = "sk_test_paystack";
    private const string CallbackUrl = "https://theadamsparadigm.com/payment/paystack-callback";

    [Theory]
    [InlineData("100", 10000)]
    [InlineData("123.45", 12345)]
    [InlineData("123.455", 12346)]
    [InlineData("0.01", 1)]
    [InlineData("0.005", 1)]
    [InlineData("1999.999", 200000)]
    public void ToSmallestUnit_ConvertsRandToCents(string amount, long expected)
    {
        Assert.Equal(expected, PaystackService.ToSmallestUnit(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    public void ToSmallestUnit_RejectsNonPositive(string amount)
    {
        Assert.Throws<ArgumentException>(() => PaystackService.ToSmallestUnit(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Fact]
    public async Task Initialize_SendsExpectedRequest()
    {
        var handler = FakeHttpMessageHandler.Returning(HttpStatusCode.OK,
            """{"status":true,"message":"Authorization URL created","data":{"authorization_url":"https://checkout.paystack.com/abc","access_code":"abc","reference":"TAP-1"}}""");
        var service = CreateService(handler);

        var result = await service.InitializeTransactionAsync("order-1", "TAP-1", "a@b.co", 250.50m);

        Assert.Equal("https://checkout.paystack.com/abc", result.AuthorizationUrl);
        Assert.Equal("abc", result.AccessCode);
        Assert.Equal("TAP-1", result.Reference);

        var (request, body) = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://api.paystack.co/transaction/initialize", request.RequestUri!.ToString());
        Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
        Assert.Equal(SecretKey, request.Headers.Authorization.Parameter);

        using var json = JsonDocument.Parse(body!);
        var root = json.RootElement;
        Assert.Equal("a@b.co", root.GetProperty("email").GetString());
        Assert.Equal(25050, root.GetProperty("amount").GetInt64());
        Assert.Equal("ZAR", root.GetProperty("currency").GetString());
        Assert.Equal("TAP-1", root.GetProperty("reference").GetString());
        Assert.Equal(CallbackUrl, root.GetProperty("callback_url").GetString());
        Assert.Equal("order-1", root.GetProperty("metadata").GetProperty("order_id").GetString());
        Assert.Equal("order-1", root.GetProperty("metadata").GetProperty("order_number").GetString());
    }

    [Fact]
    public async Task Initialize_StatusFalse_ThrowsWithPaystackMessage()
    {
        var handler = FakeHttpMessageHandler.Returning(HttpStatusCode.OK, """{"status":false,"message":"Currency not supported by merchant"}""");

        var ex = await Assert.ThrowsAsync<PaystackApiException>(
            () => CreateService(handler).InitializeTransactionAsync("order-1", "TAP-1", "a@b.co", 10m));

        Assert.Contains("Currency not supported by merchant", ex.Message);
    }

    [Fact]
    public async Task Verify_HttpError_ThrowsWithStatusAndBody()
    {
        var handler = FakeHttpMessageHandler.Returning(HttpStatusCode.Unauthorized, """{"status":false,"message":"Invalid key"}""");

        var ex = await Assert.ThrowsAsync<PaystackApiException>(
            () => CreateService(handler).VerifyTransactionAsync("TAP-1", "order-1"));

        Assert.Equal(401, ex.StatusCode);
        Assert.Contains("Invalid key", ex.Message);
        Assert.Contains("Invalid key", ex.ResponseBody);
    }

    [Fact]
    public async Task Verify_ParsesTransaction()
    {
        var handler = FakeHttpMessageHandler.Returning(HttpStatusCode.OK,
            """{"status":true,"message":"Verification successful","data":{"id":42,"status":"success","reference":"TAP-1","amount":25050,"currency":"ZAR","gateway_response":"Successful","paid_at":"2026-11-01T10:00:00.000Z"}}""");

        var result = await CreateService(handler).VerifyTransactionAsync("TAP-1");

        Assert.Equal("https://api.paystack.co/transaction/verify/TAP-1", handler.Requests.Single().Request.RequestUri!.ToString());
        Assert.Equal(42, result.Id);
        Assert.Equal("success", result.Status);
        Assert.Equal(25050, result.Amount);
        Assert.Equal("ZAR", result.Currency);
    }

    [Fact]
    public async Task MissingSecretKey_ThrowsConfigurationException()
    {
        var service = CreateService(FakeHttpMessageHandler.Returning(HttpStatusCode.OK, "{}"), secretKey: "");

        var ex = await Assert.ThrowsAsync<PaystackConfigurationException>(() => service.VerifyTransactionAsync("TAP-1"));
        Assert.Contains("Paystack__SecretKey", ex.Message);
    }

    [Fact]
    public void WebhookSignature_ValidatesHmacSha512OfRawBody()
    {
        var service = CreateService(FakeHttpMessageHandler.Returning(HttpStatusCode.OK, "{}"));
        var body = Encoding.UTF8.GetBytes("""{"event":"charge.success","data":{"reference":"TAP-1"}}""");
        var signature = Convert.ToHexString(PaystackService.ComputeSignature(body, SecretKey)).ToLowerInvariant();

        Assert.True(service.IsValidWebhookSignature(body, signature));
        Assert.False(service.IsValidWebhookSignature(body, null));
        Assert.False(service.IsValidWebhookSignature(body, ""));
        Assert.False(service.IsValidWebhookSignature(body, "not-hex"));
        Assert.False(service.IsValidWebhookSignature(body, signature[..^2]));
        Assert.False(service.IsValidWebhookSignature(Encoding.UTF8.GetBytes("tampered"), signature));
        Assert.False(service.IsValidWebhookSignature(body,
            Convert.ToHexString(PaystackService.ComputeSignature(body, "sk_test_other")).ToLowerInvariant()));
    }

    private static PaystackService CreateService(FakeHttpMessageHandler handler, string secretKey = SecretKey) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("https://api.paystack.co/") },
            Options.Create(new PaystackSettings { SecretKey = secretKey, CallbackUrl = CallbackUrl }),
            NullLogger<PaystackService>.Instance);
}
