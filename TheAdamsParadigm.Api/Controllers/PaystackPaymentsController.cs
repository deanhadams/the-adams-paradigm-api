using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheAdamsParadigm.Api.Data;
using TheAdamsParadigm.Api.Models;
using TheAdamsParadigm.Api.Models.Paystack;
using TheAdamsParadigm.Api.Services;
using TheAdamsParadigm.Api.Services.CloudCalendarService;
using TheAdamsParadigm.Api.Services.Paystack;

namespace TheAdamsParadigm.Api.Controllers;

/// <summary>
/// Paystack checkout, verification and status endpoints. Runs alongside the Yoco
/// endpoints in <see cref="PaymentsController"/>, which are unaffected.
/// </summary>
[ApiController]
[Route("api/payments/paystack")]
[Produces("application/json")]
public class PaystackPaymentsController : ControllerBase
{
    private readonly IPaystackService _paystackService;
    private readonly PaystackPaymentProcessor _paymentProcessor;
    private readonly ApplicationDbContext _context;
    private readonly ResendService _resendService;
    private readonly ICloudCalendarService _iCloudCalendarService;
    private readonly ILogger<PaystackPaymentsController> _logger;

    public PaystackPaymentsController(
        IPaystackService paystackService,
        PaystackPaymentProcessor paymentProcessor,
        ApplicationDbContext context,
        ResendService resendService,
        ICloudCalendarService iCloudCalendarService,
        ILogger<PaystackPaymentsController> logger)
    {
        _paystackService = paystackService;
        _paymentProcessor = paymentProcessor;
        _context = context;
        _resendService = resendService;
        _iCloudCalendarService = iCloudCalendarService;
        _logger = logger;
    }

    /// <summary>
    /// Creates an order and initializes a Paystack transaction for it.
    /// </summary>
    /// <remarks>
    /// Takes the same request body as the Yoco <c>POST /api/payments/create-checkout</c>.
    /// Redirect the customer to <c>paymentUrl</c>; Paystack returns them to the configured
    /// callback URL with <c>?reference=...</c>, which should then be passed to
    /// <c>GET /api/payments/paystack/verify/{reference}</c>.
    /// </remarks>
    /// <response code="200">Transaction initialized; redirect the customer to paymentUrl.</response>
    /// <response code="400">Invalid request (amount, email, client API key, duration).</response>
    /// <response code="409">The requested booking slot is no longer available.</response>
    /// <response code="502">Paystack or the booking calendar returned an error.</response>
    /// <response code="503">Paystack is not configured on the server.</response>
    [HttpPost("create-checkout")]
    [ProducesResponseType(typeof(PaystackCheckoutResponse), StatusCodes.Status200OK)]
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

            if (string.IsNullOrWhiteSpace(request.ClientApiKey))
            {
                return BadRequest(new { error = "Client API key is required." });
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new { error = "Email is required." });
            }

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
            var reference = $"TAP-{orderId}";

            var order = new Order
            {
                OrderId = orderId,
                ServiceId = request.ServiceId,
                Amount = request.Amount,
                Currency = PaystackService.Currency,
                Status = PaystackPaymentProcessor.StatusPending,
                Name = request.Name,
                Surname = request.Surname,
                Email = request.Email,
                CreatedAt = DateTime.UtcNow,
                ClientApiKey = request.ClientApiKey,
                BookingStart = request.BookingStart,
                BookingEnd = bookingEnd,
                PaymentProvider = PaymentProviders.Paystack,
                PaystackReference = reference
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync(cancellationToken);

            var transaction = await _paystackService.InitializeTransactionAsync(
                orderId,
                reference,
                request.Email,
                request.Amount,
                cancellationToken);

            order.PaymentLink = transaction.AuthorizationUrl;
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

            return Ok(new PaystackCheckoutResponse
            {
                OrderId = order.OrderId,
                OrderNumber = order.OrderId,
                Reference = transaction.Reference,
                PaymentUrl = transaction.AuthorizationUrl,
                AccessCode = transaction.AccessCode,
                Amount = order.Amount,
                Currency = order.Currency,
                BookingStart = order.BookingStart,
                BookingEnd = order.BookingEnd
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (PaystackConfigurationException ex)
        {
            _logger.LogError("Paystack create-checkout unavailable for order {OrderId}: {Message}", orderId, ex.Message);
            return StatusCode(503, new { error = "Paystack payments are not available right now." });
        }
        catch (PaystackApiException ex)
        {
            // PaystackService has already logged the status code and full response body.
            _logger.LogError(ex, "Paystack checkout initialization failed for order {OrderId}", orderId);
            return StatusCode(502, new { error = "Paystack API error", details = ex.Message });
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
            _logger.LogError(ex, "Unexpected error while creating Paystack checkout for order {OrderId}", orderId);
            return StatusCode(500, new { error = "Payment internal error." });
        }
    }

    /// <summary>
    /// Verifies a Paystack transaction server-side and updates the order's payment status.
    /// </summary>
    /// <remarks>
    /// Call this from the callback page after Paystack redirects the customer back, using the
    /// <c>reference</c> (or <c>trxref</c>) query parameter. The returned <c>status</c> is the
    /// authoritative result: <c>Paid</c>, <c>Failed</c> or <c>Pending</c>.
    /// </remarks>
    /// <response code="200">Verification ran; see status.</response>
    /// <response code="404">No Paystack order has this reference.</response>
    /// <response code="502">Paystack returned an error.</response>
    /// <response code="503">Paystack is not configured on the server.</response>
    [HttpGet("verify/{reference}")]
    [ProducesResponseType(typeof(PaystackVerifyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Verify(string reference, CancellationToken cancellationToken)
    {
        var order = await _paymentProcessor.FindOrderByReferenceAsync(reference, cancellationToken);

        if (order == null)
        {
            _logger.LogWarning("Paystack verify called for unknown reference {Reference}", reference);
            return NotFound(new { error = "Order not found.", reference });
        }

        try
        {
            var status = await _paymentProcessor.VerifyAndApplyAsync(order, "callback", cancellationToken);

            return Ok(new PaystackVerifyResponse
            {
                OrderId = order.OrderId,
                OrderNumber = order.OrderId,
                Reference = reference,
                Status = status,
                PaidAt = order.PaidAt
            });
        }
        catch (PaystackConfigurationException ex)
        {
            _logger.LogError("Paystack verify unavailable for order {OrderId}: {Message}", order.OrderId, ex.Message);
            return StatusCode(503, new { error = "Paystack payments are not available right now." });
        }
        catch (PaystackApiException ex)
        {
            _logger.LogError(ex, "Paystack verification failed for order {OrderId}, reference {Reference}", order.OrderId, reference);
            return StatusCode(502, new { error = "Could not verify payment with Paystack.", details = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error verifying Paystack payment for order {OrderId}, reference {Reference}", order.OrderId, reference);
            return StatusCode(500, new { error = "Payment internal error." });
        }
    }

    /// <summary>
    /// Returns the payment status of a Paystack order.
    /// </summary>
    /// <response code="200">The order's payment status.</response>
    /// <response code="404">No Paystack order has this ID.</response>
    [HttpGet("{orderId}")]
    [ProducesResponseType(typeof(PaystackPaymentStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPaymentStatus(string orderId, CancellationToken cancellationToken)
    {
        var order = await _context.Orders.AsNoTracking()
            .Where(o => o.OrderId == orderId && o.PaymentProvider == PaymentProviders.Paystack)
            .Select(o => new PaystackPaymentStatusResponse
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
