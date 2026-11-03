namespace TheAdamsParadigm.Api.Configuration;

// Bound from the "Payments" section (Payments__DefaultProvider on Railway). Only used
// when no ActivePaymentProvider row exists in app_settings — the database value, set
// via PUT /api/payments/provider, always wins so the provider can be switched without
// a redeploy.
public class PaymentsSettings
{
    public string DefaultProvider { get; set; } = "Yoco";
}
