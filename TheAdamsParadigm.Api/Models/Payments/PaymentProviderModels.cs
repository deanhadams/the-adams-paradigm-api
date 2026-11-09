using TheAdamsParadigm.Api.Models.Payfast;
using TheAdamsParadigm.Api.Models.Paystack;

namespace TheAdamsParadigm.Api.Models.Payments;

public static class PaymentProviderNames
{
    public const string Payfast = "Payfast";

    public static readonly IReadOnlyList<string> All = new[] { PaymentProviders.Yoco, PaymentProviders.Paystack, Payfast };

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

public static class CheckoutActions
{
    // Send the customer to PaymentUrl (Yoco, Paystack).
    public const string Redirect = "redirect";

    // POST Fields as a form to ProcessUrl in the same tab (Payfast).
    public const string Form = "form";
}

// Provider-agnostic result of POST /api/payments/checkout.
public class CheckoutResponse
{
    public string Provider { get; set; } = string.Empty;

    // "redirect" (use PaymentUrl) or "form" (POST Fields to ProcessUrl).
    public string Action { get; set; } = CheckoutActions.Redirect;

    public string OrderId { get; set; } = string.Empty;

    public string OrderNumber { get; set; } = string.Empty;

    public string? PaymentUrl { get; set; }

    public string? ProcessUrl { get; set; }

    public IReadOnlyList<PayfastFormField>? Fields { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = string.Empty;

    public DateTime? BookingStart { get; set; }

    public DateTime? BookingEnd { get; set; }
}
