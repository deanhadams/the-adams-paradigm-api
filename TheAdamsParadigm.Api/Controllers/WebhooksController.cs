using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StandardWebhooks;
using StandardWebhooks.Diagnostics;
using TheAdamsParadigm.Api.Configuration;
using TheAdamsParadigm.Api.Data;
using TheAdamsParadigm.Api.Models;
using TheAdamsParadigm.Api.Models.Calendar;
using TheAdamsParadigm.Api.Services;
using TheAdamsParadigm.Api.Services.CloudCalendarService;

namespace TheAdamsParadigm.Api.Controllers;

[ApiController]
[Route("api/webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _context;
    private readonly ProcessedWebhookStore _processedWebhookStore;
    private readonly ResendService _resendService;
    private readonly ICloudCalendarService _iCloudCalendarService;
    private readonly BookingSettings _bookingSettings;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(
        IConfiguration configuration,
        ApplicationDbContext context,
        ProcessedWebhookStore processedWebhookStore,
        ResendService resendService,
        ICloudCalendarService iCloudCalendarService,
        IOptions<BookingSettings> bookingSettings,
        ILogger<WebhooksController> logger)
    {
        _configuration = configuration;
        _context = context;
        _processedWebhookStore = processedWebhookStore;
        _resendService = resendService;
        _iCloudCalendarService = iCloudCalendarService;
        _bookingSettings = bookingSettings.Value;
        _logger = logger;
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

                var order = await _context.Orders
                    .FirstOrDefaultAsync(x => x.CheckoutId == checkoutId);

                if (order == null)
                {
                    Console.WriteLine($"No local order found for checkout: {checkoutId}");
                    return NotFound(new
                    {
                        error = "Order not found.",
                        checkoutId
                    });
                }

                if (order.Status != "Paid")
                {
                    order.Status = "Paid";
                    order.PaymentId = paymentId;
                    order.CheckoutId = checkoutId;
                    order.PaidAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    if (order.BookingStart.HasValue && order.BookingEnd.HasValue)
                    {
                        try
                        {
                            var serviceTitle = order.ServiceId.HasValue
                                ? await _context.Services.AsNoTracking()
                                    .Where(s => s.ServiceId == order.ServiceId.Value)
                                    .Select(s => s.Title)
                                    .FirstOrDefaultAsync()
                                : null;

                            var uid = await _iCloudCalendarService.CreateEventAsync(
                                _bookingSettings.ClientApiKey,
                                new CreateICloudCalendarEventRequest
                                {
                                    Summary = $"{serviceTitle ?? "Booking"} — {order.Name} {order.Surname}",
                                    Description = $"Order: {order.OrderId}\nEmail: {order.Email}",
                                    Location = "The Adams Paradigm",
                                    Start = order.BookingStart.Value,
                                    End = order.BookingEnd.Value
                                });

                            order.CalendarEventUid = uid;
                            await _context.SaveChangesAsync();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to create calendar event for order {OrderId}", order.OrderId);
                        }
                    }

                    try
                    {
                        await _resendService.SendPaymentSuccessAsync(order);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send payment confirmation emails for order {OrderId}", order.OrderId);
                    }
                }

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

                var order = await _context.Orders
                    .FirstOrDefaultAsync(x => x.CheckoutId == checkoutId);

                if (order == null)
                {
                    Console.WriteLine($"No local order found for checkout: {checkoutId}");
                    return NotFound(new
                    {
                        error = "Order not found.",
                        checkoutId
                    });
                }

                if (order.Status != "Paid")
                {
                    order.Status = "Failed";
                    order.PaymentId = paymentId;
                    order.CheckoutId = checkoutId;
                    await _context.SaveChangesAsync();
                }

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
