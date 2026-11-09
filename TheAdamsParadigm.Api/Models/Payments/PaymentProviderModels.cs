using TheAdamsParadigm.Api.Models.Paystack;

namespace TheAdamsParadigm.Api.Models.Payments;

public static class PaymentProviderNames
{
    public static readonly IReadOnlyList<string> All = new[] { PaymentProviders.Yoco, PaymentProviders.Paystack };

    // Case-insensitive match against the supported providers; returns the canonical spelling.
    public static bool TryNormalize(string? value, out string provider)
    {
        provider = All.FirstOrDefault(p => string.Equals(p, value?.Trim(), StringComparison.OrdinalIgnoreCase)) ?? string.Empty;
        return provider.Length > 0;
    }
}

public class PaymentProviderResponse
{
    public string Provider { get; set; } = string.Empty;
}

public class SetPaymentProviderRequest
{
    public string Provider { get; set; } = string.Empty;
}

// Provider-agnostic result of POST /api/payments/checkout.
public class CheckoutResponse
{
    public string Provider { get; set; } = string.Empty;

    public string OrderId { get; set; } = string.Empty;

    public string OrderNumber { get; set; } = string.Empty;

    public string? PaymentUrl { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = string.Empty;

    public DateTime? BookingStart { get; set; }

    public DateTime? BookingEnd { get; set; }
}
