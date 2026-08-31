using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheAdamsParadigm.Api.Data;
using TheAdamsParadigm.Api.Models;

namespace TheAdamsParadigm.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public OrdersController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("by-email/{email}")]
    public async Task<ActionResult<IEnumerable<OrderSummary>>> GetByEmail(string email)
    {
        var orders = await _context.Orders
            .AsNoTracking()
            .Where(o => o.Email == email)
            .Select(o => new OrderSummary
            {
                OrderNumber = o.OrderId,
                PaymentLink = o.PaymentLink,
                PaymentStatus = o.Status
            })
            .ToListAsync();

        return Ok(orders);
    }
}
