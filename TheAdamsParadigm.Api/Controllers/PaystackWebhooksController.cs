using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TheAdamsParadigm.Api.Models.Paystack;
using TheAdamsParadigm.Api.Services.Paystack;

namespace TheAdamsParadigm.Api.Controllers;

/// <summary>
/// Receives Paystack webhook events. Separate from the Yoco webhook in
/// <see cref="WebhooksController"/>, which is unaffected.
/// </summary>
[ApiController]
[Route("api/webhooks/paystack")]
public class PaystackWebhooksController : ControllerBase
{
    public const string SignatureHeader = "x-paystack-signature";

    private readonly IPaystackService _paystackService;
    private readonly PaystackPaymentProcessor _paymentProcessor;
    private readonly ILogger<PaystackWebhooksController> _logger;

    public PaystackWebhooksController(
        IPaystackService paystackService,
        PaystackPaymentProcessor paymentProcessor,
        ILogger<PaystackWebhooksController> logger)
    {
        _paystackService = paystackService;
        _paymentProcessor = paymentProcessor;
        _logger = logger;
    }

    /// <summary>
    /// Paystack webhook receiver. Set this URL as the webhook URL in the Paystack dashboard.
    /// </summary>
    /// <remarks>
    /// Requires a valid <c>x-paystack-signature</c> header (HMAC-SHA512 of the raw body keyed
    /// with the secret key). Handles <c>charge.success</c> by re-verifying the transaction with
    /// Paystack and checking amount and currency before marking the order paid. Duplicate
    /// deliveries and unknown references return 200 without side effects.
    /// </remarks>
    /// <response code="200">Event accepted (processed, duplicate, ignored or unknown reference).</response>
    /// <response code="400">Signature valid but body is not valid JSON.</response>
    /// <response code="401">Missing or invalid signature.</response>
    /// <response code="500">Processing failed; Paystack will retry.</response>
    /// <response code="503">Paystack is not configured on the server.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        // The signature is over the exact bytes Paystack sent, so read them unmodified.
        byte[] rawBody;
        using (var buffer = new MemoryStream())
        {
            await Request.Body.CopyToAsync(buffer, cancellationToken);
            rawBody = buffer.ToArray();
        }

        var signature = Request.Headers[SignatureHeader].ToString();

        try
        {
            if (!_paystackService.IsValidWebhookSignature(rawBody, signature))
            {
                _logger.LogWarning(
                    "Paystack webhook rejected: {Reason}. Body length: {BodyLength}. Remote IP: {RemoteIp}",
                    string.IsNullOrWhiteSpace(signature) ? "missing signature header" : "invalid signature",
                    rawBody.Length,
                    HttpContext.Connection.RemoteIpAddress);
                return Unauthorized();
            }
        }
        catch (PaystackConfigurationException ex)
        {
            _logger.LogError("Paystack webhook cannot be validated: {Message}", ex.Message);
            return StatusCode(503, new { error = "Paystack is not configured." });
        }

        var body = Encoding.UTF8.GetString(rawBody);

        PaystackWebhookEvent? webhookEvent;
        try
        {
            webhookEvent = JsonSerializer.Deserialize<PaystackWebhookEvent>(body);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize Paystack webhook payload. Body: {Body}", body);
            return BadRequest(new { error = "Invalid webhook JSON." });
        }

        var reference = webhookEvent?.Data?.Reference;

        _logger.LogInformation(
            "Paystack webhook received. Event: {Event}. Reference: {Reference}",
            webhookEvent?.Event, reference);

        if (webhookEvent?.Event != "charge.success")
        {
            _logger.LogInformation("Paystack webhook event {Event} ignored (not handled).", webhookEvent?.Event);
            return Ok();
        }

        if (string.IsNullOrWhiteSpace(reference))
        {
            _logger.LogWarning("Paystack charge.success webhook has no reference. Body: {Body}", body);
            return Ok();
        }

        string? orderId = null;

        try
        {
            var order = await _paymentProcessor.FindOrderByReferenceAsync(reference, cancellationToken);

            if (order == null)
            {
                _logger.LogWarning(
                    "Paystack charge.success for unknown reference {Reference} — acknowledging so Paystack stops retrying.",
                    reference);
                return Ok();
            }

            orderId = order.OrderId;

            // Never trust the webhook payload's amount/status alone — re-verify with Paystack.
            var status = await _paymentProcessor.VerifyAndApplyAsync(order, "webhook", cancellationToken);

            _logger.LogInformation(
                "Paystack webhook processed. Order: {OrderId}. Reference: {Reference}. Order status: {Status}",
                order.OrderId, reference, status);

            return Ok();
        }
        catch (Exception ex)
        {
            // Non-2xx so Paystack retries; processing is idempotent so a retry is safe.
            _logger.LogError(ex,
                "Paystack webhook processing failed. Order: {OrderId}. Reference: {Reference}. Body: {Body}",
                orderId, reference, body);
            return StatusCode(500, new { error = "Webhook processing failed." });
        }
    }
}
