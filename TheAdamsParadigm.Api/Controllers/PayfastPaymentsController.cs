using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheAdamsParadigm.Api.Data;
using TheAdamsParadigm.Api.Models;
using TheAdamsParadigm.Api.Models.Payfast;
using TheAdamsParadigm.Api.Models.Payments;
using TheAdamsParadigm.Api.Services;
using TheAdamsParadigm.Api.Services.CloudCalendarService;
using TheAdamsParadigm.Api.Services.Payfast;

namespace TheAdamsParadigm.Api.Controllers;

/// <summary>
/// Payfast checkout and status endpoints. Runs alongside the Yoco and Paystack endpoints,
/// which are unaffected.
/// </summary>
[ApiController]
[Route("api/payments/payfast")]
[Produces("application/json")]
public class PayfastPaymentsController : ControllerBase
{
    private readonly IPayfastService _payfastService;
    private readonly ApplicationDbContext _context;
    private readonly ResendService _resendService;
    private readonly ICloudCalendarService _iCloudCalendarService;
    private readonly ILogger<PayfastPaymentsController> _logger;

    public PayfastPaymentsController(
        IPayfastService payfastService,
        ApplicationDbContext context,
        ResendService resendService,
        ICloudCalendarService iCloudCalendarService,
        ILogger<PayfastPaymentsController> logger)
    {
        _payfastService = payfastService;
        _context = context;
        _resendService = resendService;
        _iCloudCalendarService = iCloudCalendarService;
        _logger = logger;
    }

    /// <summary>
    /// Creates an order and the signed Payfast checkout form for it.
    /// </summary>
    /// <remarks>
    /// Takes the same request body as the Yoco <c>POST /api/payments/create-checkout</c>.
    /// Payfast is a browser form POST: build a form with every entry in <c>fields</c> as a
    /// hidden input (in order) and submit it to <c>processUrl</c> in the same tab. Payfast
    /// confirms the payment to <c>POST /api/webhooks/payfast</c>; the return page should poll
    /// <c>GET /api/payments/payfast/{orderId}</c>, since returning is not proof of payment.
    /// The minimum amount is R5.00.
    /// </remarks>
    /// <response code="200">Checkout prepared; submit fields to processUrl.</response>
    /// <response code="400">Invalid request (amount, minimum R5.00, client API key, duration).</response>
    /// <response code="409">The requested booking slot is no longer available.</response>
    /// <response code="502">The booking calendar returned an error.</response>
    /// <response code="503">Payfast is not configured on the server.</response>
    [HttpPost("create-checkout")]
    [ProducesResponseType(typeof(PayfastCheckoutResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> CreateCheckout(CreateCheckoutRequest request, CancellationToken cancellationToken)
    {
        string? orderId = null;

        try
        {
            if (request.Amount <= 0)
            {
                return BadRequest(new { error = "Amount must be greater than zero." });
            }

            if (request.Amount < PayfastService.MinimumAmount)
            {
                return BadRequest(new { error = $"The minimum card payment is R{PayfastService.MinimumAmount:F2}." });
            }

            if (string.IsNullOrWhiteSpace(request.ClientApiKey))
            {
                return BadRequest(new { error = "Client API key is required." });
            }

            // Fail before creating an order when Payfast isn't configured.
            _payfastService.EnsureConfigured();

            // Same optional booking-slot handling as the Yoco create-checkout.
            DateTime? bookingEnd = null;

            if (request.BookingStart.HasValue)
            {
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

                bookingEnd = request.BookingStart.Value.AddMinutes(request.DurationMinutes);

                var availability = await _iCloudCalendarService.CheckAvailabilityAsync(
                    request.ClientApiKey,
                    request.BookingStart.Value,
                    bookingEnd.Value);

                if (!availability.Available)
                {
                    return Conflict(new { error = "That time slot is no longer available. Please pick another." });
                }
            }

            orderId = Guid.NewGuid().ToString();

            var order = new Order
            {
                OrderId = orderId,
                ServiceId = request.ServiceId,
                Amount = request.Amount,
                Currency = PayfastService.Currency,
                Status = PayfastItnProcessor.StatusPending,
                Name = request.Name,
                Surname = request.Surname,
                Email = request.Email,
                CreatedAt = DateTime.UtcNow,
                ClientApiKey = request.ClientApiKey,
                BookingStart = request.BookingStart,
                BookingEnd = bookingEnd,
                PaymentProvider = PaymentProviderNames.Payfast
            };

            var checkout = _payfastService.CreateCheckout(
                orderId,
                order.Amount,
                $"The Adams Paradigm order {orderId}",
                order.Name,
                order.Surname,
                order.Email);

            // There is no hosted payment link for Payfast (the form must be POSTed), so
            // PaymentLink stays empty; the order is identified by m_payment_id = OrderId.
            _context.Orders.Add(order);
            await _context.SaveChangesAsync(cancellationToken);

            try
            {
                string? serviceTitle = order.ServiceId.HasValue
                    ? (await _context.Services.AsNoTracking()
                        .Where(s => s.ServiceId == order.ServiceId.Value)
                        .Select(s => s.Title)
                        .FirstOrDefaultAsync(cancellationToken))
                    : null;

                await _resendService.SendBookingConfirmationAsync(order, serviceTitle);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send booking confirmation emails for order {OrderId}", order.OrderId);
            }

            return Ok(new PayfastCheckoutResponse
            {
                OrderId = order.OrderId,
                OrderNumber = order.OrderId,
                ProcessUrl = checkout.ProcessUrl,
                Fields = checkout.Fields,
                Amount = order.Amount,
                Currency = order.Currency,
                BookingStart = order.BookingStart,
                BookingEnd = order.BookingEnd
            });
        }
        catch (PayfastConfigurationException ex)
        {
            _logger.LogError("Payfast create-checkout unavailable: {Message}", ex.Message);
            return StatusCode(503, new { error = "Payfast payments are not available right now." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while creating Payfast checkout for order {OrderId}", orderId);
            return StatusCode(500, new { error = "Payment internal error." });
        }
    }

    /// <summary>
    /// Returns the payment status of a Payfast order.
    /// </summary>
    /// <remarks>
    /// Poll this from the return page. The status only becomes <c>Paid</c> after Payfast's
    /// ITN has passed all security checks.
    /// </remarks>
    /// <response code="200">The order's payment status.</response>
    /// <response code="404">No Payfast order has this ID.</response>
    [HttpGet("{orderId}")]
    [ProducesResponseType(typeof(PayfastPaymentStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPaymentStatus(string orderId, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";

        var order = await _context.Orders.AsNoTracking()
            .Where(o => o.OrderId == orderId && o.PaymentProvider == PaymentProviderNames.Payfast)
            .Select(o => new PayfastPaymentStatusResponse
            {
                OrderId = o.OrderId,
                OrderNumber = o.OrderId,
                Status = o.Status,
                Amount = o.Amount,
                Currency = o.Currency,
                PaidAt = o.PaidAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (order == null)
        {
            return NotFound(new { error = "Order not found.", orderId });
        }

        return Ok(order);
    }
}
