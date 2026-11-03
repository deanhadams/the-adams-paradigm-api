using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TheAdamsParadigm.Api.Configuration;
using TheAdamsParadigm.Api.Data;
using TheAdamsParadigm.Api.Models;
using TheAdamsParadigm.Api.Models.Payments;
using TheAdamsParadigm.Api.Models.Paystack;

namespace TheAdamsParadigm.Api.Services.Payments;

// Which provider NEW checkouts use. Stored in app_settings so it can be switched
// instantly; falls back to Payments__DefaultProvider, then "Yoco". Orders already in
// flight are unaffected — they complete on Order.PaymentProvider via their own webhook.
public class PaymentProviderSettingService
{
    public const string SettingKey = "ActivePaymentProvider";

    private readonly ApplicationDbContext _context;
    private readonly PaymentsSettings _paymentsSettings;
    private readonly YocoSettings _yocoSettings;
    private readonly PaystackSettings _paystackSettings;
    private readonly ILogger<PaymentProviderSettingService> _logger;

    public PaymentProviderSettingService(
        ApplicationDbContext context,
        IOptions<PaymentsSettings> paymentsSettings,
        IOptions<YocoSettings> yocoSettings,
        IOptions<PaystackSettings> paystackSettings,
        ILogger<PaymentProviderSettingService> logger)
    {
        _context = context;
        _paymentsSettings = paymentsSettings.Value;
        _yocoSettings = yocoSettings.Value;
        _paystackSettings = paystackSettings.Value;
        _logger = logger;
    }

    public async Task<string> GetActiveProviderAsync(CancellationToken cancellationToken = default)
    {
        var stored = await _context.AppSettings.AsNoTracking()
            .Where(s => s.Key == SettingKey)
            .Select(s => s.Value)
            .FirstOrDefaultAsync(cancellationToken);

        if (stored != null)
        {
            if (PaymentProviderNames.TryNormalize(stored, out var provider))
            {
                return provider;
            }

            _logger.LogWarning(
                "Stored {SettingKey} value '{StoredValue}' is not a supported provider — using the configured default.",
                SettingKey, stored);
        }

        return GetDefaultProvider();
    }

    // Returns the previous and new provider. Throws InvalidPaymentProviderException or
    // PaymentProviderNotConfiguredException without saving anything.
    public async Task<(string OldProvider, string NewProvider)> SetActiveProviderAsync(
        string? requested,
        CancellationToken cancellationToken = default)
    {
        if (!PaymentProviderNames.TryNormalize(requested, out var provider))
        {
            throw new InvalidPaymentProviderException(
                $"Unsupported payment provider '{requested}'. Allowed values: {string.Join(", ", PaymentProviderNames.All)}.");
        }

        var missing = GetMissingConfiguration(provider);
        if (missing.Count > 0)
        {
            throw new PaymentProviderNotConfiguredException(provider, missing);
        }

        var oldProvider = await GetActiveProviderAsync(cancellationToken);

        var setting = await _context.AppSettings.FirstOrDefaultAsync(s => s.Key == SettingKey, cancellationToken);
        if (setting == null)
        {
            setting = new AppSetting { Key = SettingKey };
            _context.AppSettings.Add(setting);
        }

        setting.Value = provider;
        setting.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);

        return (oldProvider, provider);
    }

    // Names of the environment variables a provider needs that are not set. Never values.
    public IReadOnlyList<string> GetMissingConfiguration(string provider)
    {
        var missing = new List<string>();

        if (provider == PaymentProviders.Yoco)
        {
            if (string.IsNullOrWhiteSpace(_yocoSettings.SecretKey)) missing.Add("Yoco__SecretKey");
        }
        else if (provider == PaymentProviders.Paystack)
        {
            if (string.IsNullOrWhiteSpace(_paystackSettings.SecretKey)) missing.Add("Paystack__SecretKey");
            if (string.IsNullOrWhiteSpace(_paystackSettings.CallbackUrl)) missing.Add("Paystack__CallbackUrl");
        }

        return missing;
    }

    private string GetDefaultProvider()
    {
        if (PaymentProviderNames.TryNormalize(_paymentsSettings.DefaultProvider, out var provider))
        {
            return provider;
        }

        _logger.LogWarning(
            "Payments__DefaultProvider '{DefaultProvider}' is not a supported provider — using {Fallback}.",
            _paymentsSettings.DefaultProvider, PaymentProviders.Yoco);

        return PaymentProviders.Yoco;
    }
}

public class InvalidPaymentProviderException : ArgumentException
{
    public InvalidPaymentProviderException(string message) : base(message)
    {
    }
}

public class PaymentProviderNotConfiguredException : InvalidOperationException
{
    public string Provider { get; }

    public IReadOnlyList<string> Missing { get; }

    public PaymentProviderNotConfiguredException(string provider, IReadOnlyList<string> missing)
        : base($"Cannot switch to {provider}: missing configuration {string.Join(", ", missing)}.")
    {
        Provider = provider;
        Missing = missing;
    }
}
