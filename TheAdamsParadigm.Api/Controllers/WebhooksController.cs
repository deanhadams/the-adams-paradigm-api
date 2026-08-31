using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using StandardWebhooks;
using StandardWebhooks.Diagnostics;
using TheAdamsParadigm.Api.Models;
using TheAdamsParadigm.Api.Services;

namespace TheAdamsParadigm.Api.Controllers;

[ApiController]
[Route("api/webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly OrderStore _orderStore;
    private readonly ProcessedWebhookStore _processedWebhookStore;

    public WebhooksController(
        IConfiguration configuration,
        OrderStore orderStore,
        ProcessedWebhookStore processedWebhookStore)
    {
        _configuration = configuration;
        _orderStore = orderStore;
        _processedWebhookStore = processedWebhookStore;
    }

    [HttpGet("yoco")]
    public IActionResult TestWebhook()
    {
        return Ok(new
        {
            message = "Yoco webhook endpoint is reachable",
            timestamp = DateTime.UtcNow
        });
    }

    [HttpPost("yoco")]
    public async Task<IActionResult> YocoWebhook()
    {
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();

        var webhookSecret = _configuration["Yoco:WebhookSecret"];

        if (string.IsNullOrWhiteSpace(webhookSecret))
        {
            Console.WriteLine("Yoco webhook secret is missing.");
            return StatusCode(500, new
            {
                error = "Yoco webhook secret is not configured."
            });
        }

        var webhookId = Request.Headers["webhook-id"].ToString();
        var webhookTimestamp = Request.Headers["webhook-timestamp"].ToString();
        var webhookSignature = Request.Headers["webhook-signature"].ToString();

        if (string.IsNullOrWhiteSpace(webhookId) ||
            string.IsNullOrWhiteSpace(webhookTimestamp) ||
            string.IsNullOrWhiteSpace(webhookSignature))
        {
            Console.WriteLine("Yoco webhook is missing required signature headers.");
            return Unauthorized();
        }

        try
        {
            var webhook = new StandardWebhook(webhookSecret);
            webhook.Verify(body, Request.Headers);

            Console.WriteLine("================================");
            Console.WriteLine("VALID YOCO WEBHOOK");
            Console.WriteLine("================================");

            var webhookEvent = JsonSerializer.Deserialize<YocoWebhookEvent>(
                body,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (webhookEvent == null)
            {
                Console.WriteLine("Unable to deserialize Yoco webhook payload.");
                return BadRequest(new { error = "Invalid webhook payload." });
            }

            Console.WriteLine($"Event ID:   {webhookEvent.Id}");
            Console.WriteLine($"Event Type: {webhookEvent.Type}");
            Console.WriteLine($"Created:    {webhookEvent.CreatedDate}");

            if (_processedWebhookStore.HasBeenProcessed(webhookEvent.Id))
            {
                Console.WriteLine($"Webhook already processed: {webhookEvent.Id}");
                return Ok();
            }

            if (webhookEvent.Type == "payment.succeeded")
            {
                var payment = webhookEvent.Payload;
                var checkoutId = payment.Metadata.CheckoutId;
                var paymentId = payment.Id;

                var order = _orderStore.GetAll()
                    .FirstOrDefault(x => x.CheckoutId == checkoutId);

                if (order == null)
                {
                    Console.WriteLine($"No local order found for checkout: {checkoutId}");
                    return NotFound(new
                    {
                        error = "Order not found.",
                        checkoutId
                    });
                }

                _orderStore.MarkAsPaid(
                    order.OrderId,
                    paymentId,
                    checkoutId);

                _processedWebhookStore.TryMarkAsProcessed(webhookEvent.Id);

                Console.WriteLine("--------------------------------");
                Console.WriteLine("PAYMENT SUCCEEDED");
                Console.WriteLine("--------------------------------");
                Console.WriteLine($"Order ID:    {order.OrderId}");
                Console.WriteLine($"Payment ID:  {paymentId}");
                Console.WriteLine($"Checkout ID: {checkoutId}");
                Console.WriteLine($"Amount:      {payment.Amount / 100m:F2} {payment.Currency}");
                Console.WriteLine($"Payment Status: {payment.Status}");
                Console.WriteLine($"Order Status: {order.Status}");
            }
            else if (webhookEvent.Type == "payment.failed")
            {
                var payment = webhookEvent.Payload;
                var checkoutId = payment.Metadata.CheckoutId;
                var paymentId = payment.Id;

                var order = _orderStore.GetAll()
                    .FirstOrDefault(x => x.CheckoutId == checkoutId);

                if (order == null)
                {
                    Console.WriteLine($"No local order found for checkout: {checkoutId}");
                    return NotFound(new
                    {
                        error = "Order not found.",
                        checkoutId
                    });
                }

                _orderStore.MarkAsFailed(
                    order.OrderId,
                    paymentId,
                    checkoutId);

                _processedWebhookStore.TryMarkAsProcessed(webhookEvent.Id);

                Console.WriteLine("--------------------------------");
                Console.WriteLine("PAYMENT FAILED");
                Console.WriteLine("--------------------------------");
                Console.WriteLine($"Order ID:    {order.OrderId}");
                Console.WriteLine($"Payment ID:  {paymentId}");
                Console.WriteLine($"Checkout ID: {checkoutId}");
                Console.WriteLine($"Amount:      {payment.Amount / 100m:F2} {payment.Currency}");
                Console.WriteLine($"Payment Status: {payment.Status}");
                Console.WriteLine($"Order Status: {order.Status}");
            }
            else
            {
                Console.WriteLine($"Unhandled Yoco event: {webhookEvent.Type}");
            }

            Console.WriteLine("================================");

            return Ok();
        }
        catch (WebhookVerificationException ex)
        {
            Console.WriteLine("================================");
            Console.WriteLine("INVALID YOCO WEBHOOK");
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine("================================");

            return Unauthorized();
        }
        catch (JsonException ex)
        {
            Console.WriteLine("================================");
            Console.WriteLine("INVALID YOCO WEBHOOK JSON");
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine("================================");

            return BadRequest(new
            {
                error = "Invalid webhook JSON."
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine("================================");
            Console.WriteLine("YOCO WEBHOOK PROCESSING ERROR");
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine("================================");

            return StatusCode(500, new
            {
                error = "Webhook processing failed."
            });
        }
    }
}
