using System.Text;
using Microsoft.AspNetCore.Mvc;
using TheAdamsParadigm.Api.Filters;
using TheAdamsParadigm.Api.Services.Payfast;

namespace TheAdamsParadigm.Api.Controllers;

/// <summary>
/// Receives Payfast ITNs (Instant Transaction Notifications). Separate from the Yoco and
/// Paystack webhooks, which are unaffected.
/// </summary>
[ApiController]
[Route("api/webhooks/payfast")]
public class PayfastWebhooksController : ControllerBase
{
    private readonly PayfastItnProcessor _itnProcessor;
    private readonly ILogger<PayfastWebhooksController> _logger;

    public PayfastWebhooksController(PayfastItnProcessor itnProcessor, ILogger<PayfastWebhooksController> logger)
    {
        _itnProcessor = itnProcessor;
        _logger = logger;
    }

    /// <summary>
    /// Payfast ITN receiver (the notify_url). Set via Payfast__NotifyUrl.
    /// </summary>
    /// <remarks>
    /// Runs the four checks from Payfast's docs before touching the order: source IP in
    /// Payfast's ranges, signature (with the passphrase), amount_gross against the order, and
    /// server-side validation with Payfast. Only <c>COMPLETE</c> marks the order Paid;
    /// <c>FAILED</c>/<c>CANCELLED</c> mark it Failed/Cancelled; anything else leaves it
    /// Pending. Duplicate ITNs are acknowledged without side effects.
    /// </remarks>
    /// <response code="200">Accepted (processed, duplicate, or unknown order).</response>
    /// <response code="400">Invalid signature, merchant, amount, or Payfast rejected the ITN.</response>
    /// <response code="403">The request did not come from a Payfast IP address.</response>
    /// <response code="502">Payfast could not be reached to validate the ITN; Payfast will retry.</response>
    /// <response code="503">Payfast is not configured on the server.</response>
    [HttpPost]
    [DisableFormValueModelBinding]
    [Consumes("application/x-www-form-urlencoded")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        string body;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
        {
            body = await reader.ReadToEndAsync(cancellationToken);
        }

        // After UseForwardedHeaders this is the client address Railway's edge proxy saw.
        var remoteIp = HttpContext.Connection.RemoteIpAddress;

        PayfastItnResult result;
        try
        {
            result = await _itnProcessor.ProcessAsync(body, remoteIp, cancellationToken);
        }
        catch (Exception ex)
        {
            // Non-2xx so Payfast retries; processing is idempotent so a retry is safe.
            _logger.LogError(ex,
                "Payfast ITN processing failed. Source IP: {RemoteIp}. X-Forwarded-For: {ForwardedFor}",
                remoteIp, Request.Headers["X-Forwarded-For"].ToString());
            return StatusCode(500, new { error = "ITN processing failed." });
        }

        if (result.Outcome == PayfastItnOutcome.InvalidSource)
        {
            // Helps diagnose proxy setups where the wrong hop is taken as the client IP.
            _logger.LogWarning(
                "Payfast ITN source check details. RemoteIp: {RemoteIp}. X-Forwarded-For: {ForwardedFor}",
                remoteIp, Request.Headers["X-Forwarded-For"].ToString());
        }

        return result.Outcome switch
        {
            PayfastItnOutcome.Processed or PayfastItnOutcome.UnknownOrder => Ok(),
            PayfastItnOutcome.InvalidSource => StatusCode(403),
            PayfastItnOutcome.NotConfigured => StatusCode(503),
            PayfastItnOutcome.ValidationUnavailable => StatusCode(502),
            _ => BadRequest()
        };
    }
}
