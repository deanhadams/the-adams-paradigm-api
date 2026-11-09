namespace TheAdamsParadigm.Api.Configuration;

// Bound from the "Payfast" section. On Railway these come from environment variables
// (Payfast__MerchantId, Payfast__MerchantKey, Payfast__Passphrase, Payfast__UseSandbox,
// Payfast__ReturnUrl, Payfast__CancelUrl, Payfast__NotifyUrl). Not validated at startup
// so the app (and Yoco/Paystack) keeps running without them — PayfastService checks on
// first use instead.
public class PayfastSettings
{
    public const string LiveHost = "www.payfast.co.za";
    public const string SandboxHost = "sandbox.payfast.co.za";

    public string MerchantId { get; set; } = string.Empty;
    public string MerchantKey { get; set; } = string.Empty;

    // The "salt passphrase" set in the Payfast dashboard. Secret: never sent to the
    // browser, logged or returned.
    public string Passphrase { get; set; } = string.Empty;

    public bool UseSandbox { get; set; }

    // Frontend pages the customer returns to, e.g.
    // https://theadamsparadigm.com/payment/payfast-return and .../payment/payfast-cancel.
    // The order id is appended as ?orderId=... per transaction.
    public string ReturnUrl { get; set; } = string.Empty;
    public string CancelUrl { get; set; } = string.Empty;

    // Public URL of POST /api/webhooks/payfast (the ITN endpoint).
    public string NotifyUrl { get; set; } = string.Empty;

    public string Host => UseSandbox ? SandboxHost : LiveHost;
    public string ProcessUrl => $"https://{Host}/eng/process";
    public string ValidateUrl => $"https://{Host}/eng/query/validate";
}
