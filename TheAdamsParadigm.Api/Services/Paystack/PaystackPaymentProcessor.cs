using Microsoft.EntityFrameworkCore;
using TheAdamsParadigm.Api.Data;
using TheAdamsParadigm.Api.Models;
using TheAdamsParadigm.Api.Models.Calendar;
using TheAdamsParadigm.Api.Models.Paystack;
using TheAdamsParadigm.Api.Services.CloudCalendarService;

namespace TheAdamsParadigm.Api.Services.Paystack;

// Applies a server-side Paystack verification result to an order. Shared by the
// callback verify endpoint and the webhook, which commonly arrive at the same moment
// for the same payment — the Pending→Paid transition is claimed with a single
// conditional UPDATE so only one of them ever runs the post-payment side effects
// (calendar event + payment-success email), even across restarts or instances.
public class PaystackPaymentProcessor
{
    public const string StatusPaid = "Paid";
    public const string StatusFailed = "Failed";
    public const string StatusPending = "Pending";

    private readonly IPaystackService _paystackService;
    private readonly ApplicationDbContext _context;
    private readonly ResendService _resendService;
    private readonly ICloudCalendarService _iCloudCalendarService;
    private readonly ILogger<PaystackPaymentProcessor> _logger;

    public PaystackPaymentProcessor(
        IPaystackService paystackService,
        ApplicationDbContext context,
        ResendService resendService,
        ICloudCalendarService iCloudCalendarService,
        ILogger<PaystackPaymentProcessor> logger)
    {
        _paystackService = paystackService;
        _context = context;
        _resendService = resendService;
        _iCloudCalendarService = iCloudCalendarService;
        _logger = logger;
    }

    public Task<Order?> FindOrderByReferenceAsync(string reference, CancellationToken cancellationToken = default) =>
        _context.Orders.FirstOrDefaultAsync(
            o => o.PaystackReference == reference && o.PaymentProvider == PaymentProviders.Paystack,
            cancellationToken);

    // Verifies the transaction with Paystack and updates the order. Returns the order's
    // resulting status. `source` is only used for logging ("callback" / "webhook").
    public async Task<string> VerifyAndApplyAsync(Order order, string source, CancellationToken cancellationToken = default)
    {
        if (order.Status == StatusPaid)
        {
            _logger.LogInformation(
                "Paystack {Source}: order {OrderId} (reference {Reference}) is already Paid — skipping.",
                source, order.OrderId, order.PaystackReference);
            return StatusPaid;
        }

        var reference = order.PaystackReference
            ?? throw new InvalidOperationException($"Order {order.OrderId} has no Paystack reference.");

        var transaction = await _paystackService.VerifyTransactionAsync(reference, order.OrderId, cancellationToken);

        switch (transaction.Status)
        {
            case "success":
                return await ApplySuccessAsync(order, transaction, source, cancellationToken);

            case "failed":
                var failed = await _context.Orders
                    .Where(o => o.OrderId == order.OrderId && o.Status != StatusPaid)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(o => o.Status, StatusFailed)
                        .SetProperty(o => o.PaymentId, transaction.Id.ToString()),
                        cancellationToken);

                _logger.LogWarning(
                    "Paystack {Source}: payment FAILED for order {OrderId}. Reference: {Reference}. Gateway response: {GatewayResponse}. Rows updated: {Rows}",
                    source, order.OrderId, reference, transaction.GatewayResponse, failed);

                await _context.Entry(order).ReloadAsync(cancellationToken);
                return order.Status;

            default:
                // abandoned / ongoing / pending / processing / queued / reversed: leave the
                // order as-is — the customer may still complete it, and a reversal after
                // payment needs a human, not an automatic status flip.
                _logger.LogInformation(
                    "Paystack {Source}: order {OrderId} not paid yet. Reference: {Reference}. Paystack status: {PaystackStatus}. Gateway response: {GatewayResponse}",
                    source, order.OrderId, reference, transaction.Status, transaction.GatewayResponse);
                return order.Status;
        }
    }

    private async Task<string> ApplySuccessAsync(
        Order order,
        PaystackTransactionData transaction,
        string source,
        CancellationToken cancellationToken)
    {
        var expectedCents = PaystackService.ToSmallestUnit(order.Amount);

        if (transaction.Amount != expectedCents ||
            !string.Equals(transaction.Currency, order.Currency, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogError(
                "Paystack {Source}: AMOUNT/CURRENCY MISMATCH for order {OrderId}. Reference: {Reference}. Expected {ExpectedCents} {ExpectedCurrency}, Paystack reported {ActualCents} {ActualCurrency}. Order NOT marked as paid.",
                source, order.OrderId, transaction.Reference, expectedCents, order.Currency, transaction.Amount, transaction.Currency);
            return order.Status;
        }

        // Captured as a local so EF sends it as a parameter through the column's UTC
        // converter. Inline DateTime.UtcNow is translated by Npgsql to SQL now()
        // (timestamptz), which can't be assigned to this timestamp-without-time-zone column.
        var paidAt = DateTime.UtcNow;

        var claimed = await _context.Orders
            .Where(o => o.OrderId == order.OrderId && o.Status != StatusPaid)
            .ExecuteUpdateAsync(s => s
                .SetProperty(o => o.Status, StatusPaid)
                .SetProperty(o => o.PaymentId, transaction.Id.ToString())
                .SetProperty(o => o.PaidAt, paidAt),
                cancellationToken);

        await _context.Entry(order).ReloadAsync(cancellationToken);

        if (claimed == 0)
        {
            _logger.LogInformation(
                "Paystack {Source}: order {OrderId} was marked Paid concurrently by another request — skipping side effects.",
                source, order.OrderId);
            return order.Status;
        }

        _logger.LogInformation(
            "Paystack {Source}: PAYMENT SUCCEEDED. Order: {OrderId}. Reference: {Reference}. Paystack transaction: {TransactionId}. Amount: {AmountInCents} {Currency}",
            source, order.OrderId, transaction.Reference, transaction.Id, transaction.Amount, transaction.Currency);

        await CreateCalendarEventAsync(order);

        try
        {
            await _resendService.SendPaymentSuccessAsync(order);
            _logger.LogInformation("Paystack {Source}: payment confirmation emails sent for order {OrderId}.", source, order.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send payment confirmation emails for order {OrderId}", order.OrderId);
        }

        return StatusPaid;
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
