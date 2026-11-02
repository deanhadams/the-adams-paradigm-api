namespace TheAdamsParadigm.Api.Configuration;

// Bound from the "Paystack" section. On Railway these come from environment variables
// (Paystack__SecretKey, Paystack__PublicKey, Paystack__BaseUrl, Paystack__CallbackUrl).
// Deliberately not validated at startup so the app (and Yoco) keeps running when
// Paystack isn't configured — PaystackService checks on first use instead.
public class PaystackSettings
{
    public const string DefaultBaseUrl = "https://api.paystack.co";

    public string SecretKey { get; set; } = string.Empty;
    public string PublicKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = DefaultBaseUrl;

    // Frontend page Paystack redirects the customer back to after payment,
    // e.g. https://theadamsparadigm.com/payment/paystack-callback
    public string CallbackUrl { get; set; } = string.Empty;
}
