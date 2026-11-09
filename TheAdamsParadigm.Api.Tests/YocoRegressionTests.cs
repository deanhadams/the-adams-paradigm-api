using System.Net;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using StandardWebhooks;
using TheAdamsParadigm.Api.Configuration;
using TheAdamsParadigm.Api.Controllers;
using TheAdamsParadigm.Api.Data;
using TheAdamsParadigm.Api.Models;
using TheAdamsParadigm.Api.Services;
using TheAdamsParadigm.Api.Services.CloudCalendarService;
using Xunit;

namespace TheAdamsParadigm.Api.Tests;

// Pins the existing Yoco behaviour so the Paystack work (or anything later) can't
// silently change it: the outgoing create-checkout request and the webhook paths.
public class YocoRegressionTests
{
    private const string YocoSecretKey = "sk_test_yoco";

    // Standard Webhooks secret: "whsec_" + base64 key.
    private static readonly string WebhookSecret = "whsec_" + Convert.ToBase64String(Encoding.UTF8.GetBytes("yoco-test-webhook-secret-key-32b"));

    [Fact]
    public async Task CreateCheckout_SendsSameRequestToYoco()
    {
        var handler = FakeHttpMessageHandler.Returning(HttpStatusCode.OK,
            """{"id":"ch_123","redirectUrl":"https://c.yoco.com/checkout/ch_123","status":"created"}""");
        var service = CreateYocoService(handler);

        var response = await service.CreateCheckoutAsync("order-1", 123.455m);

        var (request, body) = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://payments.yoco.com/api/checkouts", request.RequestUri!.ToString());
        Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
        Assert.Equal(YocoSecretKey, request.Headers.Authorization.Parameter);
        Assert.True(Guid.TryParse(request.Headers.GetValues("Idempotency-Key").Single(), out _));
        Assert.Equal("""{"amount":12346,"currency":"ZAR","metadata":{"orderId":"order-1"}}""", body);
        Assert.Contains("ch_123", response);
    }

    [Fact]
    public async Task CreateCheckout_NonSuccess_ThrowsHttpRequestExceptionWithBody()
    {
        var handler = FakeHttpMessageHandler.Returning(HttpStatusCode.BadRequest, """{"message":"bad"}""");
        var service = CreateYocoService(handler);

        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => service.CreateCheckoutAsync("order-1", 10m));
        Assert.Equal("""Yoco returned 400: {"message":"bad"}""", ex.Message);
    }

    [Fact]
    public async Task Webhook_MissingSignatureHeaders_Returns401()
    {
        using var context = CreateContext();
        var controller = CreateWebhooksController(context, body: "{}", signed: false);

        var result = await controller.YocoWebhook();

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Webhook_PaymentSucceeded_MarksOrderPaidOnce()
    {
        using var context = CreateContext();
        context.Orders.Add(new Order
        {
            OrderId = "order-1",
            Amount = 100m,
            Status = "Pending",
            CheckoutId = "ch_123",
            Email = "",
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var body = """
            {"id":"evt_1","type":"payment.succeeded","createdDate":"2026-11-01T10:00:00Z",
             "payload":{"id":"p_1","amount":10000,"currency":"ZAR","status":"succeeded",
                        "metadata":{"checkoutId":"ch_123"}}}
            """;
        var store = new ProcessedWebhookStore();

        var result = await CreateWebhooksController(context, body, signed: true, store).YocoWebhook();

        Assert.IsType<OkResult>(result);
        var order = await context.Orders.AsNoTracking().SingleAsync();
        Assert.Equal("Paid", order.Status);
        Assert.Equal("p_1", order.PaymentId);
        Assert.NotNull(order.PaidAt);
        Assert.Equal("Yoco", order.PaymentProvider);
        Assert.True(store.HasBeenProcessed("evt_1"));

        // Redelivery of the same event is acknowledged without reprocessing.
        var replay = await CreateWebhooksController(context, body, signed: true, store).YocoWebhook();
        Assert.IsType<OkResult>(replay);
    }

    [Fact]
    public async Task Webhook_UnknownCheckout_StillReturns404()
    {
        using var context = CreateContext();
        var body = """{"id":"evt_2","type":"payment.succeeded","payload":{"id":"p_2","metadata":{"checkoutId":"missing"}}}""";

        var result = await CreateWebhooksController(context, body, signed: true).YocoWebhook();

        Assert.IsType<NotFoundObjectResult>(result);
    }

    private static YocoService CreateYocoService(FakeHttpMessageHandler handler) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("https://payments.yoco.com/") },
            Options.Create(new YocoSettings { SecretKey = YocoSecretKey }),
            NullLogger<YocoService>.Instance);

    private static ApplicationDbContext CreateContext() => new TestDbContext();

    private static WebhooksController CreateWebhooksController(
        ApplicationDbContext context,
        string body,
        bool signed,
        ProcessedWebhookStore? store = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Yoco:WebhookSecret"] = WebhookSecret })
            .Build();

        // Resend calls succeed; there's no booking slot so the calendar service isn't hit.
        var resend = new ResendService(
            new HttpClient(FakeHttpMessageHandler.Returning(HttpStatusCode.OK, "{}")) { BaseAddress = new Uri("https://api.resend.com/") },
            Options.Create(new ResendSettings()));
        var calendar = new ICloudCalendarService(
            new HttpClient(),
            new ICloudSettings(),
            context,
            new ClientCredentialProtector(new EphemeralDataProtectionProvider()));

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));

        if (signed)
        {
            var id = "msg_" + Guid.NewGuid().ToString("N");
            var timestamp = DateTimeOffset.UtcNow;
            httpContext.Request.Headers["webhook-id"] = id;
            httpContext.Request.Headers["webhook-timestamp"] = timestamp.ToUnixTimeSeconds().ToString();
            httpContext.Request.Headers["webhook-signature"] = new StandardWebhook(WebhookSecret).Sign(id, timestamp, body);
        }

        return new WebhooksController(
            configuration,
            context,
            store ?? new ProcessedWebhookStore(),
            resend,
            calendar,
            NullLogger<WebhooksController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };
    }
}
