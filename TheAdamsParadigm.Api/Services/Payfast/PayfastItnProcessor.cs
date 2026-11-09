using System.Globalization;
using System.Net;
using Microsoft.EntityFrameworkCore;
using TheAdamsParadigm.Api.Data;
using TheAdamsParadigm.Api.Models;
using TheAdamsParadigm.Api.Models.Calendar;
using TheAdamsParadigm.Api.Models.Payments;
using TheAdamsParadigm.Api.Services.CloudCalendarService;

namespace TheAdamsParadigm.Api.Services.Payfast;

public enum PayfastItnOutcome
{
    Processed,
    UnknownOrder,
    NotConfigured,
    InvalidSource,
    InvalidSignature,
    MerchantMismatch,
    AmountMismatch,
    ValidationRejected,
    ValidationUnavailable,
    Malformed
}

public record PayfastItnResult(PayfastItnOutcome Outcome, string? OrderId = null, string? OrderStatus = null);

// Handles a Payfast ITN (Instant Transaction Notification). Runs every check the docs
// require — source IP, signature, (merchant id), amount, and the server-side validation
// call — before touching the order. Pending→Paid is claimed with one conditional UPDATE so
// duplicate ITNs never re-send emails or re-create calendar events.
public class PayfastItnProcessor
{
    public const string StatusPaid = "Paid";
    public const string StatusFailed = "Failed";
    public const string StatusCancelled = "Cancelled";
    public const string StatusPending = "Pending";

    private readonly IPayfastService _payfastService;
    private readonly ApplicationDbContext _context;
    private readonly ResendService _resendService;
    private readonly ICloudCalendarService _iCloudCalendarService;
    private readonly ILogger<PayfastItnProcessor> _logger;

    public PayfastItnProcessor(
        IPayfastService payfastService,
        ApplicationDbContext context,
        ResendService resendService,
        ICloudCalendarService iCloudCalendarService,
        ILogger<PayfastItnProcessor> logger)
    {
        _payfastService = payfastService;
        _context = context;
        _resendService = resendService;
        _iCloudCalendarService = iCloudCalendarService;
        _logger = logger;
    }

    public async Task<PayfastItnResult> ProcessAsync(string rawBody, IPAddress? remoteIp, CancellationToken cancellationToken = default)
    {
        try
        {
            _payfastService.EnsureConfigured();
        }
        catch (PayfastConfigurationException ex)
        {
            _logger.LogError("Payfast ITN cannot be processed: {Message}", ex.Message);
            return new PayfastItnResult(PayfastItnOutcome.NotConfigured);
        }

        var fields = PayfastService.ParseFormBody(rawBody);
        string Field(string name) => fields.FirstOrDefault(f => f.Key == name).Value ?? string.Empty;

        // Identifiers and status only: the payload also carries customer PII and our merchant id.
        var orderId = Field("m_payment_id");
        var pfPaymentId = Field("pf_payment_id");
        var paymentStatus = Field("payment_status");
        var amountGross = Field("amount_gross");

        _logger.LogInformation(
            "Payfast ITN received. Order: {OrderId}. pf_payment_id: {PfPaymentId}. Status: {PaymentStatus}. Amount: {AmountGross}. Source IP: {RemoteIp}",
            orderId, pfPaymentId, paymentStatus, amountGross, remoteIp);

        if (!await _payfastService.IsValidSourceIpAsync(remoteIp, cancellationToken))
        {
            _logger.LogWarning(
                "Payfast ITN REJECTED: source IP {RemoteIp} is not a Payfast address. Order: {OrderId}. pf_payment_id: {PfPaymentId}. Fields: {FieldNames}",
                remoteIp, orderId, pfPaymentId, string.Join(",", fields.Select(f => f.Key)));
            return new PayfastItnResult(PayfastItnOutcome.InvalidSource, orderId);
        }

        if (!_payfastService.IsValidItnSignature(fields))
        {
            _logger.LogWarning(
                "Payfast ITN REJECTED: invalid or missing signature. Order: {OrderId}. pf_payment_id: {PfPaymentId}. Fields: {FieldNames}",
                orderId, pfPaymentId, string.Join(",", fields.Select(f => f.Key)));
            return new PayfastItnResult(PayfastItnOutcome.InvalidSignature, orderId);
        }

        if (!_payfastService.IsOwnMerchantId(Field("merchant_id")))
        {
            _logger.LogWarning("Payfast ITN REJECTED: merchant_id does not match our account. Order: {OrderId}. pf_payment_id: {PfPaymentId}", orderId, pfPaymentId);
            return new PayfastItnResult(PayfastItnOutcome.MerchantMismatch, orderId);
        }

        if (string.IsNullOrWhiteSpace(orderId))
        {
            _logger.LogWarning("Payfast ITN has no m_payment_id. pf_payment_id: {PfPaymentId}", pfPaymentId);
            return new PayfastItnResult(PayfastItnOutcome.Malformed);
        }

        var order = await _context.Orders.FirstOrDefaultAsync(
            o => o.OrderId == orderId && o.PaymentProvider == PaymentProviderNames.Payfast,
            cancellationToken);

        if (order == null)
        {
            _logger.LogWarning("Payfast ITN for unknown order {OrderId} — acknowledging so Payfast stops retrying.", orderId);
            return new PayfastItnResult(PayfastItnOutcome.UnknownOrder, orderId);
        }

        if (!decimal.TryParse(amountGross, NumberStyles.Number, CultureInfo.InvariantCulture, out var paid) ||
            Math.Abs(paid - order.Amount) > 0.01m)
        {
            _logger.LogError(
                "Payfast ITN REJECTED: AMOUNT MISMATCH for order {OrderId}. Expected {Expected}, ITN amount_gross {AmountGross}. pf_payment_id: {PfPaymentId}. Order NOT updated.",
                orderId, PayfastService.FormatAmount(order.Amount), amountGross, pfPaymentId);
            return new PayfastItnResult(PayfastItnOutcome.AmountMismatch, orderId, order.Status);
        }

        if (order.Status == StatusPaid)
        {
            _logger.LogInformation("Payfast ITN: order {OrderId} is already Paid — duplicate delivery, skipping.", orderId);
            return new PayfastItnResult(PayfastItnOutcome.Processed, orderId, StatusPaid);
        }

        bool confirmed;
        try
        {
            confirmed = await _payfastService.ConfirmWithPayfastAsync(fields, orderId, cancellationToken);
        }
        catch (PayfastApiException)
        {
            // Already logged by PayfastService; a non-200 makes Payfast retry the ITN.
            return new PayfastItnResult(PayfastItnOutcome.ValidationUnavailable, orderId, order.Status);
        }

        if (!confirmed)
        {
            _logger.LogError("Payfast ITN REJECTED: Payfast did not confirm the notification as VALID. Order: {OrderId}. pf_payment_id: {PfPaymentId}", orderId, pfPaymentId);
            return new PayfastItnResult(PayfastItnOutcome.ValidationRejected, orderId, order.Status);
        }

        var status = paymentStatus.Trim().ToUpperInvariant() switch
        {
            "COMPLETE" => await MarkPaidAsync(order, pfPaymentId, cancellationToken),
            "FAILED" => await MarkUnpaidAsync(order, StatusFailed, pfPaymentId, cancellationToken),
            "CANCELLED" => await MarkUnpaidAsync(order, StatusCancelled, pfPaymentId, cancellationToken),
            _ => LeavePending(order, paymentStatus)
        };

        return new PayfastItnResult(PayfastItnOutcome.Processed, orderId, status);
    }

    private async Task<string> MarkPaidAsync(Order order, string pfPaymentId, CancellationToken cancellationToken)
    {
        // Local, so EF sends a parameter through the column's UTC converter (an inline
        // DateTime.UtcNow becomes SQL now(), which this timestamp column rejects).
        var paidAt = DateTime.UtcNow;

        var claimed = await _context.Orders
            .Where(o => o.OrderId == order.OrderId && o.Status != StatusPaid)
            .ExecuteUpdateAsync(s => s
                .SetProperty(o => o.Status, StatusPaid)
                .SetProperty(o => o.PayfastPaymentId, pfPaymentId)
                .SetProperty(o => o.PaymentId, pfPaymentId)
                .SetProperty(o => o.PaidAt, paidAt),
                cancellationToken);

        await _context.Entry(order).ReloadAsync(cancellationToken);

        if (claimed == 0)
        {
            _logger.LogInformation("Payfast ITN: order {OrderId} was marked Paid concurrently — skipping side effects.", order.OrderId);
            return order.Status;
        }

        _logger.LogInformation(
            "Payfast ITN: PAYMENT SUCCEEDED. Order: {OrderId}. pf_payment_id: {PfPaymentId}. Amount: {Amount} {Currency}",
            order.OrderId, pfPaymentId, PayfastService.FormatAmount(order.Amount), order.Currency);

        await CreateCalendarEventAsync(order);

        try
        {
            await _resendService.SendPaymentSuccessAsync(order);
            _logger.LogInformation("Payfast ITN: payment confirmation emails sent for order {OrderId}.", order.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send payment confirmation emails for order {OrderId}", order.OrderId);
        }

        return StatusPaid;
    }

    private async Task<string> MarkUnpaidAsync(Order order, string status, string pfPaymentId, CancellationToken cancellationToken)
    {
        var updated = await _context.Orders
            .Where(o => o.OrderId == order.OrderId && o.Status != StatusPaid)
            .ExecuteUpdateAsync(s => s
                .SetProperty(o => o.Status, status)
                .SetProperty(o => o.PayfastPaymentId, pfPaymentId),
                cancellationToken);

        await _context.Entry(order).ReloadAsync(cancellationToken);

        _logger.LogWarning(
            "Payfast ITN: payment {Status} for order {OrderId}. pf_payment_id: {PfPaymentId}. Rows updated: {Rows}",
            status.ToUpperInvariant(), order.OrderId, pfPaymentId, updated);

        return order.Status;
    }

    private string LeavePending(Order order, string paymentStatus)
    {
        _logger.LogInformation(
            "Payfast ITN: unhandled payment_status '{PaymentStatus}' for order {OrderId} — leaving it as {Status}.",
            paymentStatus, order.OrderId, order.Status);
        return order.Status;
    }

    // Mirrors the calendar step of the Yoco payment.succeeded handler in WebhooksController.
    private async Task CreateCalendarEventAsync(Order order)
    {
        if (!order.BookingStart.HasValue || !order.BookingEnd.HasValue)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(order.ClientApiKey))
        {
            _logger.LogError(
                "Order {OrderId} has a booking but no ClientApiKey was captured at checkout — skipping calendar event creation.",
                order.OrderId);
            return;
        }

        try
        {
            var serviceTitle = order.ServiceId.HasValue
                ? await _context.Services.AsNoTracking()
                    .Where(s => s.ServiceId == order.ServiceId.Value)
                    .Select(s => s.Title)
                    .FirstOrDefaultAsync()
                : null;

            var uid = await _iCloudCalendarService.CreateEventAsync(
                order.ClientApiKey,
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
}
