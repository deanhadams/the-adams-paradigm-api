using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TheAdamsParadigm.Api.Models;
using TheAdamsParadigm.Api.Services;

namespace TheAdamsParadigm.Api.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly YocoService _yocoService;
    private readonly OrderStore _orderStore;

    public PaymentsController(YocoService yocoService, OrderStore orderStore)
    {
        _yocoService = yocoService;
        _orderStore = orderStore;
    }

    [HttpPost("create-checkout")]
    public async Task<IActionResult> CreateCheckout(CreateCheckoutRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.OrderId))
            {
                return BadRequest(new { error = "OrderId is required." });
            }

            if (request.Amount <= 0)
            {
                return BadRequest(new { error = "Amount must be greater than zero." });
            }

            var order = new Order
            {
                OrderId = request.OrderId,
                Amount = request.Amount,
                Currency = "ZAR",
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            if (!_orderStore.Create(order))
            {
                return Conflict(new
                {
                    error = $"Order '{request.OrderId}' already exists."
                });
            }

            var yocoResponse = await _yocoService.CreateCheckoutAsync(
                request.OrderId,
                request.Amount);

            using var document = JsonDocument.Parse(yocoResponse);
            var checkoutId = document.RootElement
                .GetProperty("id")
                .GetString();

            order.CheckoutId = checkoutId;

            return Content(yocoResponse, "application/json");
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
    public IActionResult GetPaymentStatus(string orderId)
    {
        if (!_orderStore.TryGet(orderId, out var order))
        {
            return NotFound(new
            {
                error = "Order not found.",
                orderId
            });
        }

        return Ok(order);
    }
}
