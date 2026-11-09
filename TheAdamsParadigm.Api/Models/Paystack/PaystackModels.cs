using System.Text.Json.Serialization;

namespace TheAdamsParadigm.Api.Models.Paystack;

public static class PaymentProviders
{
    public const string Yoco = "Yoco";
    public const string Paystack = "Paystack";
}

// Generic Paystack response envelope: { "status": bool, "message": string, "data": {...} }
public class PaystackEnvelope<T>
{
    [JsonPropertyName("status")]
    public bool Status { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("data")]
    public T? Data { get; set; }
}

public class PaystackInitializeData
{
    [JsonPropertyName("authorization_url")]
    public string AuthorizationUrl { get; set; } = string.Empty;

    [JsonPropertyName("access_code")]
    public string AccessCode { get; set; } = string.Empty;

    [JsonPropertyName("reference")]
    public string Reference { get; set; } = string.Empty;
}

public class PaystackTransactionData
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    // "success", "failed", "abandoned", "ongoing", "pending", "processing", "queued", "reversed"
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("reference")]
    public string Reference { get; set; } = string.Empty;

    // Smallest currency unit (cents for ZAR).
    [JsonPropertyName("amount")]
    public long Amount { get; set; }

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    [JsonPropertyName("gateway_response")]
    public string? GatewayResponse { get; set; }

    [JsonPropertyName("paid_at")]
    public DateTime? PaidAt { get; set; }
}

public class PaystackWebhookEvent
{
    [JsonPropertyName("event")]
    public string Event { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public PaystackTransactionData? Data { get; set; }
}

public class PaystackCheckoutResponse
{
    public string OrderId { get; set; } = string.Empty;

    // The order's public number — the same value as OrderId, matching how
    // /api/orders/by-email exposes it.
    public string OrderNumber { get; set; } = string.Empty;

    public string Reference { get; set; } = string.Empty;

    public string PaymentUrl { get; set; } = string.Empty;

    public string AccessCode { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string Currency { get; set; } = string.Empty;

    public DateTime? BookingStart { get; set; }

    public DateTime? BookingEnd { get; set; }
}

public class PaystackVerifyResponse
{
    public string OrderId { get; set; } = string.Empty;

    public string OrderNumber { get; set; } = string.Empty;

    public string Reference { get; set; } = string.Empty;

    // Order status after verification: "Paid", "Failed" or "Pending".
    public string Status { get; set; } = string.Empty;

    public DateTime? PaidAt { get; set; }
}

public class PaystackPaymentStatusResponse
{
    public string OrderId { get; set; } = string.Empty;

    public string OrderNumber { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string Currency { get; set; } = string.Empty;

    public DateTime? PaidAt { get; set; }
}
