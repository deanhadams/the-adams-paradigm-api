using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TheAdamsParadigm.Api.Configuration;
using TheAdamsParadigm.Api.Data;
using TheAdamsParadigm.Api.Models;
using TheAdamsParadigm.Api.Services;
using TheAdamsParadigm.Api.Services.CloudCalendarService;

namespace TheAdamsParadigm.Api.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly YocoService _yocoService;
    private readonly ApplicationDbContext _context;
    private readonly ResendService _resendService;
    private readonly ICloudCalendarService _iCloudCalendarService;
    private readonly BookingSettings _bookingSettings;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        YocoService yocoService,
        ApplicationDbContext context,
        ResendService resendService,
        ICloudCalendarService iCloudCalendarService,
        IOptions<BookingSettings> bookingSettings,
        ILogger<PaymentsController> logger)
    {
        _yocoService = yocoService;
        _context = context;
        _resendService = resendService;
        _iCloudCalendarService = iCloudCalendarService;
        _bookingSettings = bookingSettings.Value;
        _logger = logger;
    }

    [HttpPost("create-checkout")]
    public async Task<IActionResult> CreateCheckout(CreateCheckoutRequest request)
    {
        try
        {
            if (request.Amount <= 0)
            {
                return BadRequest(new { error = "Amount must be greater than zero." });
            }

            if (request.DurationMinutes <= 0)
            {
                return BadRequest(new { error = "Duration must be greater than zero." });
            }

            var nowInBookingTimeZone = TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                ICloudCalendarService.BookingTimeZone);

            if (request.BookingStart <= nowInBookingTimeZone)
            {
                return BadRequest(new { error = "Please select a booking time in the future." });
            }

            var bookingEnd = request.BookingStart.AddMinutes(request.DurationMinutes);

            var availability = await _iCloudCalendarService.CheckAvailabilityAsync(
                _bookingSettings.ClientApiKey,
                request.BookingStart,
                bookingEnd);

            if (!availability.Available)
            {
                return Conflict(new { error = "That time slot is no longer available. Please pick another." });
            }

            var orderId = Guid.NewGuid().ToString();

            var order = new Order
            {
                OrderId = orderId,
                ServiceId = request.ServiceId,
                Amount = request.Amount,
                Currency = "ZAR",
                Status = "Pending",
                Name = request.Name,
                Surname = request.Surname,
                Email = request.Email,
                CreatedAt = DateTime.UtcNow,
                BookingStart = request.BookingStart,
                BookingEnd = bookingEnd
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            var yocoResponse = await _yocoService.CreateCheckoutAsync(
                orderId,
                request.Amount);

            using var document = JsonDocument.Parse(yocoResponse);
            var checkoutId = document.RootElement
                .GetProperty("id")
                .GetString();
            var redirectUrl = document.RootElement
                .GetProperty("redirectUrl")
                .GetString();
            var yocoStatus = document.RootElement
                .GetProperty("status")
                .GetString();

            order.CheckoutId = checkoutId;
            order.PaymentLink = redirectUrl;
            await _context.SaveChangesAsync();

            try
            {
                string? serviceTitle = order.ServiceId.HasValue
                    ? (await _context.Services.AsNoTracking()
                        .Where(s => s.ServiceId == order.ServiceId.Value)
                        .Select(s => s.Title)
                        .FirstOrDefaultAsync())
                    : null;

                await _resendService.SendBookingConfirmationAsync(order, serviceTitle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send booking confirmation emails for order {OrderId}", order.OrderId);
            }

            return Ok(new CreateCheckoutResponse
            {
                OrderId = order.OrderId,
                CheckoutId = checkoutId,
                PaymentUrl = redirectUrl,
                Amount = order.Amount,
                Currency = order.Currency,
                YocoStatus = yocoStatus,
                BookingStart = order.BookingStart!.Value,
                BookingEnd = order.BookingEnd!.Value
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(502, new
            {
                error = "Yoco API error",
                details = ex.Message
            });
        }
        catch (Exception ex) when (
            ex is ClientNotFoundException ||
            ex is ClientCloudCredentialsMissingException)
        {
            _logger.LogError(ex, "Booking calendar is not configured correctly.");

            return StatusCode(502, new
            {
                error = "Booking calendar is currently unavailable. Please try again shortly."
            });
        }
    }

    [HttpPost("register-webhook")]
    public async Task<IActionResult> RegisterWebhook([FromQuery] string webhookUrl)
    {
        try
        {
            var result = await _yocoService.RegisterWebhookAsync(webhookUrl);
            return Content(result, "application/json");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (HttpRequestException ex)
        {
            return StatusCode(502, new { error = ex.Message });
        }
    }

    [HttpGet("{orderId}")]
    public async Task<IActionResult> GetPaymentStatus(string orderId)
    {
        var order = await _context.Orders.AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null)
        {
            return NotFound(new
            {
                error = "Order not found.",
                orderId
            });
        }

        return Ok(order);
    }

    [HttpPost("{orderId}/test-payment-email")]
    public async Task<IActionResult> TestPaymentEmail(string orderId)
    {
        var order = await _context.Orders.AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrderId == orderId);

        if (order == null)
        {
            return NotFound(new
            {
                error = "Order not found.",
                orderId
            });
        }

        try
        {
            await _resendService.SendPaymentSuccessAsync(order);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return StatusCode(502, new
            {
                error = "Failed to send payment confirmation email.",
                details = ex.Message
            });
        }
    }
}
