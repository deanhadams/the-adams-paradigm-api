using System.Collections.Concurrent;
using TheAdamsParadigm.Api.Models;

namespace TheAdamsParadigm.Api.Services;

public class OrderStore
{
    private readonly ConcurrentDictionary<string, Order> _orders = new();

    public bool Create(Order order)
    {
        return _orders.TryAdd(order.OrderId, order);
    }

    public bool TryGet(string orderId, out Order? order)
    {
        return _orders.TryGetValue(orderId, out order);
    }

    public IEnumerable<Order> GetAll()
    {
        return _orders.Values;
    }

    public bool MarkAsPaid(string orderId, string paymentId, string checkoutId)
    {
        if (!_orders.TryGetValue(orderId, out var order))
        {
            return false;
        }

        if (order.Status == "Paid")
        {
            return true;
        }

        order.Status = "Paid";
        order.PaymentId = paymentId;
        order.CheckoutId = checkoutId;
        order.PaidAt = DateTime.UtcNow;

        return true;
    }

    public bool MarkAsFailed(string orderId, string paymentId, string checkoutId)
    {
        if (!_orders.TryGetValue(orderId, out var order))
        {
            return false;
        }

        if (order.Status == "Paid")
        {
            return true;
        }

        order.Status = "Failed";
        order.PaymentId = paymentId;
        order.CheckoutId = checkoutId;

        return true;
    }
}
