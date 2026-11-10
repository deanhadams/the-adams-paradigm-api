using Microsoft.AspNetCore.Mvc;
using TheAdamsParadigm.Api.Filters;
using TheAdamsParadigm.Api.Models;
using TheAdamsParadigm.Api.Models.Payfast;
using TheAdamsParadigm.Api.Models.Payments;
using TheAdamsParadigm.Api.Models.Paystack;
using TheAdamsParadigm.Api.Services.Payments;

namespace TheAdamsParadigm.Api.Controllers;

/// <summary>
/// Chooses which payment provider new checkouts use, and a provider-agnostic checkout
/// endpoint that dispatches to it. The provider-specific endpoints and both webhooks stay
/// active regardless of the setting, so in-flight orders complete on the provider they
/// started with.
/// </summary>
[ApiController]
[Route("api/payments")]
[Produces("application/json")]
public class PaymentProviderController : ControllerBase
{
    private readonly PaymentProviderSettingService _settingService;
    private readonly ICheckoutProviderGateway _checkoutGateway;
    private readonly ILogger<PaymentProviderController> _logger;

    public PaymentProviderController(
        PaymentProviderSettingService settingService,
        ICheckoutProviderGateway checkoutGateway,
        ILogger<PaymentProviderController> logger)
    {
        _settingService = settingService;
        _checkoutGateway = checkoutGateway;
        _logger = logger;
    }

    /// <summary>
    /// Returns the payment provider new checkouts currently use.
    /// </summary>
    /// <response code="200">The active provider: "Yoco" or "Paystack".</response>
    [HttpGet("provider")]
    [ProducesResponseType(typeof(PaymentProviderResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProvider(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";

        var provider = await _settingService.GetActiveProviderAsync(cancellationToken);

        return Ok(new PaymentProviderResponse { Provider = provider });
    }

    /// <summary>
    /// Switches the payment provider used by new checkouts. Requires the X-Admin-Key header.
    /// </summary>
    /// <remarks>
    /// Accepts "Yoco" or "Paystack" (case-insensitive). Refuses to switch to a provider whose
    /// required configuration is missing, listing the missing environment variables.
    /// Existing orders are unaffected.
    /// </remarks>
    /// <response code="200">Switched; returns the new active provider.</response>
    /// <response code="400">Unsupported provider, or its configuration is missing.</response>
    /// <response code="401">Missing or invalid X-Admin-Key.</response>
    /// <response code="503">Admin endpoints are disabled (Admin__ApiKey not set).</response>
    [HttpPut("provider")]
    [RequireAdminKey]
    [ProducesResponseType(typeof(PaymentProviderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> SetProvider(SetPaymentProviderRequest request, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";

        try
        {
            var (oldProvider, newProvider) = await _settingService.SetActiveProviderAsync(request.Provider, cancellationToken);

            _logger.LogInformation(
                "Active payment provider changed from {OldProvider} to {NewProvider} via admin key. Remote IP: {RemoteIp}. User agent: {UserAgent}",
                oldProvider,
                newProvider,
                HttpContext.Connection.RemoteIpAddress,
                Request.Headers.UserAgent.ToString());

            return Ok(new PaymentProviderResponse { Provider = newProvider });
        }
        catch (InvalidPaymentProviderException ex)
        {
            _logger.LogWarning("Rejected payment provider switch: {Reason}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (PaymentProviderNotConfiguredException ex)
        {
            _logger.LogWarning("Rejected payment provider switch: {Reason}", ex.Message);
            return BadRequest(new { error = ex.Message, missing = ex.Missing });
        }
    }

    /// <summary>
    /// Creates an order and a checkout with the currently active payment provider.
    /// </summary>
    /// <remarks>
    /// Takes the same body as <c>POST /api/payments/create-checkout</c> and runs the existing
    /// Yoco, Paystack or Payfast create-checkout logic unchanged. Error responses from that
    /// logic are passed through as-is. <c>action</c> says what to do next: <c>redirect</c>
    /// (open <c>paymentUrl</c>; same tab for Paystack) or <c>form</c> (Payfast: POST every
    /// entry of <c>fields</c> as a hidden input to <c>processUrl</c> in the same tab).
    /// </remarks>
    /// <response code="200">Checkout created with the provider named in the response.</response>
    /// <response code="400">Invalid request.</response>
    /// <response code="409">The requested booking slot is no longer available.</response>
    /// <response code="502">The payment provider or booking calendar returned an error.</response>
    /// <response code="503">The active provider is not configured on the server.</response>
    [HttpPost("checkout")]
    [ProducesResponseType(typeof(CheckoutResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Checkout(CreateCheckoutRequest request, CancellationToken cancellationToken)
    {
        var provider = await _settingService.GetActiveProviderAsync(cancellationToken);

        _logger.LogInformation("Dispatching checkout to active payment provider {Provider}", provider);

        if (provider == PaymentProviderNames.Payfast)
        {
            var result = await _checkoutGateway.CreatePayfastCheckoutAsync(request, ControllerContext);

            if (result is OkObjectResult { Value: PayfastCheckoutResponse payfast })
            {
                return Ok(new CheckoutResponse
                {
                    Provider = PaymentProviderNames.Payfast,
                    Action = CheckoutActions.Form,
                    OrderId = payfast.OrderId,
                    OrderNumber = payfast.OrderNumber,
                    ProcessUrl = payfast.ProcessUrl,
                    Fields = payfast.Fields,
                    Amount = payfast.Amount,
                    Currency = payfast.Currency,
                    BookingStart = payfast.BookingStart,
                    BookingEnd = payfast.BookingEnd
                });
            }

            return result;
        }

        if (provider == PaymentProviders.Paystack)
        {
            var result = await _checkoutGateway.CreatePaystackCheckoutAsync(request, ControllerContext);

            if (result is OkObjectResult { Value: PaystackCheckoutResponse paystack })
            {
                return Ok(new CheckoutResponse
                {
                    Provider = PaymentProviders.Paystack,
                    OrderId = paystack.OrderId,
                    OrderNumber = paystack.OrderNumber,
                    PaymentUrl = paystack.PaymentUrl,
                    Amount = paystack.Amount,
                    Currency = paystack.Currency,
                    BookingStart = paystack.BookingStart,
                    BookingEnd = paystack.BookingEnd
                });
            }

            return result;
        }

        var yocoResult = await _checkoutGateway.CreateYocoCheckoutAsync(request, ControllerContext);

        if (yocoResult is OkObjectResult { Value: CreateCheckoutResponse yoco })
        {
            return Ok(new CheckoutResponse
            {
                Provider = PaymentProviders.Yoco,
                OrderId = yoco.OrderId,
                OrderNumber = yoco.OrderId,
                PaymentUrl = yoco.PaymentUrl,
                Amount = yoco.Amount,
                Currency = yoco.Currency,
                BookingStart = yoco.BookingStart,
                BookingEnd = yoco.BookingEnd
            });
        }

        return yocoResult;
    }
}
