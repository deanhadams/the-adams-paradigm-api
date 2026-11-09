namespace TheAdamsParadigm.Api.Models.Payfast;

// One hidden input of the Payfast checkout form. Kept as an ordered list (not a
// dictionary) so the browser posts the fields in the same order they were signed.
public class PayfastFormField
{
    public string Name { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public PayfastFormField()
    {
    }

    public PayfastFormField(string name, string value)
    {
        Name = name;
        Value = value;
    }
}

public class PayfastCheckout
{
    public string ProcessUrl { get; set; } = string.Empty;

    public IReadOnlyList<PayfastFormField> Fields { get; set; } = Array.Empty<PayfastFormField>();
}

public class PayfastCheckoutResponse
{
    public string Provider { get; set; } = "Payfast";

    public string OrderId { get; set; } = string.Empty;

    // Same value as OrderId, matching how /api/orders/by-email exposes it.
    public string OrderNumber { get; set; } = string.Empty;

    // Payfast's process URL (sandbox or live). The browser POSTs Fields here.
    public string ProcessUrl { get; set; } = string.Empty;

    // The signed form fields, in signing order, including "signature".
    public IReadOnlyList<PayfastFormField> Fields { get; set; } = Array.Empty<PayfastFormField>();

    public decimal Amount { get; set; }

    public string Currency { get; set; } = string.Empty;

    public DateTime? BookingStart { get; set; }

    public DateTime? BookingEnd { get; set; }
}

public class PayfastPaymentStatusResponse
{
    public string OrderId { get; set; } = string.Empty;

    public string OrderNumber { get; set; } = string.Empty;

    // "Paid", "Failed", "Cancelled" or "Pending".
    public string Status { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string Currency { get; set; } = string.Empty;

    public DateTime? PaidAt { get; set; }
}
