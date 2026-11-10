using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using TheAdamsParadigm.Api.Configuration;
using TheAdamsParadigm.Api.Models.Payfast;

namespace TheAdamsParadigm.Api.Services.Payfast;

public interface IPayfastService
{
    // Throws PayfastConfigurationException naming the missing environment variables.
    void EnsureConfigured();

    PayfastCheckout CreateCheckout(
        string orderId,
        decimal amount,
        string itemName,
        string? firstName,
        string? lastName,
        string? email);

    bool IsOwnMerchantId(string? merchantId);

    bool IsValidItnSignature(IReadOnlyList<KeyValuePair<string, string>> itnFields);

    Task<bool> IsValidSourceIpAsync(IPAddress? remoteIp, CancellationToken cancellationToken = default);

    // Posts the ITN back to Payfast's /eng/query/validate. True only for a "VALID" reply.
    // Throws PayfastApiException when Payfast can't be reached or returns a non-2xx status.
    Task<bool> ConfirmWithPayfastAsync(
        IReadOnlyList<KeyValuePair<string, string>> itnFields,
        string? orderId,
        CancellationToken cancellationToken = default);
}

// Payfast Custom Integration (one-off payments), per https://developers.payfast.co.za/docs:
// a signed HTML form POSTed by the browser to /eng/process, confirmed by an ITN to our
// notify_url that must pass four checks (signature, source IP, amount, server validation).
public class PayfastService : IPayfastService
{
    public const string Currency = "ZAR";

    // Live payments below R5.00 are rejected by Payfast ("Going Live" in the docs).
    public const decimal MinimumAmount = 5.00m;

    // Checkout form fields in the order the docs list them; the signature must be built in
    // this order (not alphabetically — that's the separate server API's format).
    public static readonly IReadOnlyList<string> FieldOrder = new[]
    {
        "merchant_id", "merchant_key", "return_url", "cancel_url", "notify_url", "fica_idnumber",
        "name_first", "name_last", "email_address", "cell_number",
        "m_payment_id", "amount", "item_name", "item_description",
        "custom_int1", "custom_int2", "custom_int3", "custom_int4", "custom_int5",
        "custom_str1", "custom_str2", "custom_str3", "custom_str4", "custom_str5",
        "email_confirmation", "confirmation_address", "payment_method"
    };

    // "Ports and IP addresses" section of the docs.
    public static readonly IReadOnlyList<System.Net.IPNetwork> ValidSourceNetworks = new[]
    {
        System.Net.IPNetwork.Parse("197.97.145.144/28"),
        System.Net.IPNetwork.Parse("41.74.179.192/27"),
        System.Net.IPNetwork.Parse("102.216.36.0/28"),
        System.Net.IPNetwork.Parse("102.216.36.128/28"),
        System.Net.IPNetwork.Parse("144.126.193.139/32")
    };

    // "Check that the notification has come from a valid Payfast domain" section.
    public static readonly IReadOnlyList<string> ValidSourceHosts = new[]
    {
        "www.payfast.co.za", "w1w.payfast.co.za", "w2w.payfast.co.za", "sandbox.payfast.co.za"
    };

    private readonly HttpClient _httpClient;
    private readonly PayfastSettings _settings;
    private readonly ILogger<PayfastService> _logger;

    // Swappable for tests; DNS lookups of ValidSourceHosts.
    public Func<string, CancellationToken, Task<IPAddress[]>> ResolveHostAsync { get; set; } = Dns.GetHostAddressesAsync;

    public PayfastService(HttpClient httpClient, IOptions<PayfastSettings> settings, ILogger<PayfastService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public static IReadOnlyList<string> GetMissingConfiguration(PayfastSettings settings)
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(settings.MerchantId)) missing.Add("Payfast__MerchantId");
        if (string.IsNullOrWhiteSpace(settings.MerchantKey)) missing.Add("Payfast__MerchantKey");
        if (string.IsNullOrWhiteSpace(settings.Passphrase)) missing.Add("Payfast__Passphrase");
        if (string.IsNullOrWhiteSpace(settings.ReturnUrl)) missing.Add("Payfast__ReturnUrl");
        if (string.IsNullOrWhiteSpace(settings.CancelUrl)) missing.Add("Payfast__CancelUrl");
        if (string.IsNullOrWhiteSpace(settings.NotifyUrl)) missing.Add("Payfast__NotifyUrl");
        return missing;
    }

    public void EnsureConfigured()
    {
        var missing = GetMissingConfiguration(_settings);
        if (missing.Count > 0)
        {
            throw new PayfastConfigurationException(missing);
        }
    }

    public PayfastCheckout CreateCheckout(
        string orderId,
        decimal amount,
        string itemName,
        string? firstName,
        string? lastName,
        string? email)
    {
        EnsureConfigured();

        if (amount < MinimumAmount)
        {
            throw new ArgumentException($"Payfast payments must be at least R{MinimumAmount:F2}.");
        }

        var values = new Dictionary<string, string?>
        {
            ["merchant_id"] = _settings.MerchantId,
            ["merchant_key"] = _settings.MerchantKey,
            ["return_url"] = AppendOrderId(_settings.ReturnUrl, orderId),
            ["cancel_url"] = AppendOrderId(_settings.CancelUrl, orderId),
            ["notify_url"] = _settings.NotifyUrl,
            ["name_first"] = Truncate(firstName, 100),
            ["name_last"] = Truncate(lastName, 100),
            ["email_address"] = Truncate(email, 100),
            ["m_payment_id"] = orderId,
            ["amount"] = FormatAmount(amount),
            ["item_name"] = Truncate(itemName, 100),
        };

        // Only non-blank fields are sent and signed; values are trimmed exactly as the
        // signature trims them, so what the browser posts is what was signed.
        var fields = FieldOrder
            .Where(name => !string.IsNullOrWhiteSpace(values.GetValueOrDefault(name)))
            .Select(name => new PayfastFormField(name, values[name]!.Trim()))
            .ToList();

        var paramString = BuildParamString(fields.Select(f => new KeyValuePair<string, string>(f.Name, f.Value)));
        fields.Add(new PayfastFormField("signature", Md5Hex(AppendPassphrase(paramString, _settings.Passphrase.Trim()))));

        _logger.LogInformation(
            "Payfast checkout prepared. Order: {OrderId}. Amount: {Amount} {Currency}. Sandbox: {Sandbox}",
            orderId, FormatAmount(amount), Currency, _settings.UseSandbox);

        return new PayfastCheckout { ProcessUrl = _settings.ProcessUrl, Fields = fields };
    }

    public bool IsOwnMerchantId(string? merchantId) =>
        string.Equals(merchantId?.Trim(), _settings.MerchantId.Trim(), StringComparison.Ordinal);

    // ITN signature: every posted field in the order received, up to (excluding) "signature",
    // blanks included, url-encoded without trimming, then &passphrase=..., MD5 lower-case hex.
    public bool IsValidItnSignature(IReadOnlyList<KeyValuePair<string, string>> itnFields)
    {
        EnsureConfigured();

        var provided = itnFields.FirstOrDefault(f => f.Key == "signature").Value;
        if (string.IsNullOrWhiteSpace(provided))
        {
            return false;
        }

        var expected = Md5Hex(AppendPassphrase(BuildItnParamString(itnFields), _settings.Passphrase));

        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(expected),
            Encoding.ASCII.GetBytes(provided.Trim().ToLowerInvariant()));
    }

    public async Task<bool> IsValidSourceIpAsync(IPAddress? remoteIp, CancellationToken cancellationToken = default)
    {
        if (remoteIp == null)
        {
            return false;
        }

        var ip = remoteIp.IsIPv4MappedToIPv6 ? remoteIp.MapToIPv4() : remoteIp;

        if (ValidSourceNetworks.Any(network => network.Contains(ip)))
        {
            return true;
        }

        foreach (var host in ValidSourceHosts)
        {
            try
            {
                var addresses = await ResolveHostAsync(host, cancellationToken);
                if (addresses.Any(a => (a.IsIPv4MappedToIPv6 ? a.MapToIPv4() : a).Equals(ip)))
                {
                    return true;
                }
            }
            catch (Exception ex) when (ex is System.Net.Sockets.SocketException or ArgumentException)
            {
                _logger.LogWarning("Could not resolve Payfast host {Host} for ITN source check: {Message}", host, ex.Message);
            }
        }

        return false;
    }

    public async Task<bool> ConfirmWithPayfastAsync(
        IReadOnlyList<KeyValuePair<string, string>> itnFields,
        string? orderId,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        // The docs post the same parameter string used for the signature, without the passphrase.
        using var request = new HttpRequestMessage(HttpMethod.Post, _settings.ValidateUrl)
        {
            Content = new StringContent(BuildItnParamString(itnFields), Encoding.UTF8, "application/x-www-form-urlencoded")
        };

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Payfast ITN validation could not reach Payfast. Order: {OrderId}", orderId);
            throw new PayfastApiException($"Could not reach Payfast to validate the ITN: {ex.Message}", null, ex);
        }

        using (response)
        {
            var body = (await response.Content.ReadAsStringAsync(cancellationToken)).Trim();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Payfast ITN validation failed. Status: {StatusCode}. Order: {OrderId}. Response body: {ResponseBody}",
                    (int)response.StatusCode, orderId, body);
                throw new PayfastApiException($"Payfast ITN validation returned {(int)response.StatusCode}.", (int)response.StatusCode);
            }

            if (body != "VALID")
            {
                _logger.LogError("Payfast ITN validation rejected the notification. Order: {OrderId}. Response body: {ResponseBody}", orderId, body);
                return false;
            }

            return true;
        }
    }

    // "decimal" in ZAR with two places and a dot, e.g. 100.00 (docs: number_format(x, 2, '.', '')).
    public static string FormatAmount(decimal amount) =>
        decimal.Round(amount, 2, MidpointRounding.AwayFromZero).ToString("0.00", CultureInfo.InvariantCulture);

    // PHP urlencode(), which the docs' signature code uses: A-Z a-z 0-9 - _ . unchanged, space
    // as '+', every other UTF-8 byte as upper-case %XX. (WebUtility.UrlEncode differs for
    // characters such as ! * ( ) ~, which would break the signature.)
    public static string PhpUrlEncode(string value)
    {
        var builder = new StringBuilder(value.Length * 3);
        foreach (var b in Encoding.UTF8.GetBytes(value))
        {
            var c = (char)b;
            if (c is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '-' or '_' or '.')
            {
                builder.Append(c);
            }
            else if (c == ' ')
            {
                builder.Append('+');
            }
            else
            {
                builder.Append('%').Append(b.ToString("X2"));
            }
        }

        return builder.ToString();
    }

    // Parses an application/x-www-form-urlencoded body, preserving field order.
    public static IReadOnlyList<KeyValuePair<string, string>> ParseFormBody(string body)
    {
        var fields = new List<KeyValuePair<string, string>>();
        foreach (var pair in body.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = pair.IndexOf('=');
            var key = separator < 0 ? pair : pair[..separator];
            var value = separator < 0 ? string.Empty : pair[(separator + 1)..];
            fields.Add(new KeyValuePair<string, string>(WebUtility.UrlDecode(key), WebUtility.UrlDecode(value)));
        }

        return fields;
    }

    public static string BuildParamString(IEnumerable<KeyValuePair<string, string>> fields) =>
        string.Join("&", fields.Select(f => $"{f.Key}={PhpUrlEncode(f.Value)}"));

    public static string BuildItnParamString(IReadOnlyList<KeyValuePair<string, string>> itnFields) =>
        BuildParamString(itnFields.TakeWhile(f => f.Key != "signature"));

    public static string Md5Hex(string value) =>
        Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static string AppendPassphrase(string paramString, string passphrase) =>
        string.IsNullOrEmpty(passphrase) ? paramString : $"{paramString}&passphrase={PhpUrlEncode(passphrase)}";

    private static string AppendOrderId(string url, string orderId) =>
        $"{url}{(url.Contains('?') ? '&' : '?')}orderId={Uri.EscapeDataString(orderId)}";

    private static string? Truncate(string? value, int maxLength)
    {
        var trimmed = value?.Trim();
        return trimmed is { Length: > 0 } && trimmed.Length > maxLength ? trimmed[..maxLength] : trimmed;
    }
}

public class PayfastConfigurationException : InvalidOperationException
{
    public IReadOnlyList<string> Missing { get; }

    public PayfastConfigurationException(IReadOnlyList<string> missing)
        : base($"Payfast is not configured. Missing: {string.Join(", ", missing)}.")
    {
        Missing = missing;
    }
}

public class PayfastApiException : Exception
{
    public int? StatusCode { get; }

    public PayfastApiException(string message, int? statusCode, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }
}
