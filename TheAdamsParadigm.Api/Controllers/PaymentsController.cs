using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheAdamsParadigm.Api.Data;
using TheAdamsParadigm.Api.Models;
using TheAdamsParadigm.Api.Services;

namespace TheAdamsParadigm.Api.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly YocoService _yocoService;
    private readonly ApplicationDbContext _context;

    public PaymentsController(YocoService yocoService, ApplicationDbContext context)
    {
        _yocoService = yocoService;
        _context = context;
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
                CreatedAt = DateTime.UtcNow
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

            order.CheckoutId = checkoutId;
            order.PaymentLink = redirectUrl;
            await _context.SaveChangesAsync();

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
}
