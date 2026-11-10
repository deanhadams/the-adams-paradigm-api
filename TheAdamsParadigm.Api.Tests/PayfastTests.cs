using System.Net;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TheAdamsParadigm.Api.Configuration;
using TheAdamsParadigm.Api.Controllers;
using TheAdamsParadigm.Api.Data;
using TheAdamsParadigm.Api.Models;
using TheAdamsParadigm.Api.Models.Payments;
using TheAdamsParadigm.Api.Services;
using TheAdamsParadigm.Api.Services.CloudCalendarService;
using TheAdamsParadigm.Api.Services.Payfast;
using TheAdamsParadigm.Api.Services.Payments;
using Xunit;

namespace TheAdamsParadigm.Api.Tests;

// Synthetic values only — never real merchant credentials.
public static class PayfastTestData
{
    public const string MerchantId = "11111111";
    public const string MerchantKey = "testmerchantkey";
    public const string Passphrase = "test passphrase!*~";
    public const string OrderId = "3f2c1a9e-0000-4000-8000-000000000001";

    // A Payfast source address from the documented ranges, and one that isn't.
    public static readonly IPAddress PayfastIp = IPAddress.Parse("197.97.145.150");
    public static readonly IPAddress OtherIp = IPAddress.Parse("203.0.113.9");

    public static PayfastSettings Settings(bool sandbox = true) => new()
    {
        MerchantId = MerchantId,
        MerchantKey = MerchantKey,
        Passphrase = Passphrase,
        UseSandbox = sandbox,
        ReturnUrl = "https://example.test/payment/payfast-return",
        CancelUrl = "https://example.test/payment/payfast-cancel",
        NotifyUrl = "https://api.example.test/api/webhooks/payfast"
    };

    public static PayfastService CreateService(
        FakeHttpMessageHandler? validateHandler = null,
        PayfastSettings? settings = null,
        IPAddress[]? resolvedHostIps = null) =>
        new(
            new HttpClient(validateHandler ?? FakeHttpMessageHandler.Returning(HttpStatusCode.OK, "VALID")),
            Options.Create(settings ?? Settings()),
            NullLogger<PayfastService>.Instance)
        {
            ResolveHostAsync = (_, _) => Task.FromResult(resolvedHostIps ?? Array.Empty<IPAddress>())
        };

    // ITN fields in the order Payfast posts them (signature appended by Sign).
    public static List<KeyValuePair<string, string>> ItnFields(string status = "COMPLETE", string amountGross = "150.00", string merchantId = MerchantId) => new()
    {
        new("m_payment_id", OrderId),
        new("pf_payment_id", "1089250"),
        new("payment_status", status),
        new("item_name", $"The Adams Paradigm order {OrderId}"),
        new("item_description", ""),
        new("amount_gross", amountGross),
        new("amount_fee", "-4.60"),
        new("amount_net", "145.40"),
        new("custom_str1", ""),
        new("name_first", "Zoë"),
        new("name_last", "O'Neil (Jr)"),
        new("email_address", "zoe+test@example.test"),
        new("merchant_id", merchantId)
    };

    public static string Sign(List<KeyValuePair<string, string>> fields, string passphrase = Passphrase)
    {
        var signature = PayfastService.Md5Hex($"{PayfastService.BuildItnParamString(fields)}&passphrase={PayfastService.PhpUrlEncode(passphrase)}");
        return ToBody(fields.Append(new("signature", signature)));
    }

    public static string ToBody(IEnumerable<KeyValuePair<string, string>> fields) =>
        string.Join("&", fields.Select(f => $"{f.Key}={Uri.EscapeDataString(f.Value)}"));
}

public class PayfastServiceTests
{
    [Theory]
    [InlineData("150", "150.00")]
    [InlineData("150.5", "150.50")]
    [InlineData("99.995", "100.00")]
    [InlineData("1234567.891", "1234567.89")]
    [InlineData("5", "5.00")]
    public void FormatAmount_TwoDecimalsInvariant(string amount, string expected)
    {
        var previous = Thread.CurrentThread.CurrentCulture;
        try
        {
            // A comma-decimal culture must not leak into the amount.
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
            Assert.Equal(expected, PayfastService.FormatAmount(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)));
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previous;
        }
    }

    [Fact]
    public void PhpUrlEncode_MatchesPhpUrlencode()
    {
        // Expected value produced by an independent implementation of PHP's urlencode().
        Assert.Equal("a+b%21%2A%27%28%29%7E%2B%C3%A9%2F%3A%3F%3D%26", PayfastService.PhpUrlEncode("a b!*'()~+é/:?=&"));
    }

    [Fact]
    public void CreateCheckout_SignatureMatchesIndependentVector()
    {
        var checkout = PayfastTestData.CreateService().CreateCheckout(
            PayfastTestData.OrderId, 150m, $"The Adams Paradigm order {PayfastTestData.OrderId}", "Zoë", "O'Neil (Jr)", "zoe+test@example.test");

        // Computed outside .NET with the docs' PHP algorithm (see PR notes).
        Assert.Equal("3f195787412c30d2217efb1200004e82", checkout.Fields.Last().Value);
    }

    [Fact]
    public void CreateCheckout_FieldsInDocumentedOrder_BlanksOmitted_SignatureLast()
    {
        var checkout = PayfastTestData.CreateService().CreateCheckout(
            PayfastTestData.OrderId, 150m, "Item", "  Zoë ", null, "");

        Assert.Equal(
            new[] { "merchant_id", "merchant_key", "return_url", "cancel_url", "notify_url", "name_first", "m_payment_id", "amount", "item_name", "signature" },
            checkout.Fields.Select(f => f.Name));
        Assert.Equal("Zoë", checkout.Fields.Single(f => f.Name == "name_first").Value);
        Assert.Equal("150.00", checkout.Fields.Single(f => f.Name == "amount").Value);
        Assert.Equal(PayfastTestData.OrderId, checkout.Fields.Single(f => f.Name == "m_payment_id").Value);
        Assert.Equal($"https://example.test/payment/payfast-return?orderId={PayfastTestData.OrderId}", checkout.Fields.Single(f => f.Name == "return_url").Value);
        Assert.Equal($"https://example.test/payment/payfast-cancel?orderId={PayfastTestData.OrderId}", checkout.Fields.Single(f => f.Name == "cancel_url").Value);
        Assert.DoesNotContain(checkout.Fields, f => f.Name == "passphrase" || f.Value.Contains(PayfastTestData.Passphrase));
        Assert.DoesNotContain(checkout.Fields, f => f.Name == "payment_method");
    }

    [Theory]
    [InlineData(true, "https://sandbox.payfast.co.za/eng/process")]
    [InlineData(false, "https://www.payfast.co.za/eng/process")]
    public void CreateCheckout_PicksSandboxOrLiveUrl(bool sandbox, string expected)
    {
        var service = PayfastTestData.CreateService(settings: PayfastTestData.Settings(sandbox));

        Assert.Equal(expected, service.CreateCheckout(PayfastTestData.OrderId, 10m, "Item", null, null, null).ProcessUrl);
    }

    [Fact]
    public void CreateCheckout_BelowMinimum_Throws()
    {
        Assert.Throws<ArgumentException>(() => PayfastTestData.CreateService().CreateCheckout(PayfastTestData.OrderId, 4.99m, "Item", null, null, null));
    }

    [Fact]
    public void MissingConfig_ThrowsNamingVariables_WithoutValues()
    {
        var settings = PayfastTestData.Settings();
        settings.Passphrase = "";
        settings.NotifyUrl = " ";

        var ex = Assert.Throws<PayfastConfigurationException>(() => PayfastTestData.CreateService(settings: settings).EnsureConfigured());

        Assert.Equal(new[] { "Payfast__Passphrase", "Payfast__NotifyUrl" }, ex.Missing);
        Assert.DoesNotContain(PayfastTestData.MerchantKey, ex.Message);
    }

    [Fact]
    public void ItnSignature_MatchesIndependentVector()
    {
        var fields = PayfastTestData.ItnFields();
        fields.Add(new("signature", "170b10e8233cb6ce77817a48790fca65"));

        Assert.True(PayfastTestData.CreateService().IsValidItnSignature(fields));
    }

    [Fact]
    public void ItnSignature_RejectsTamperingWrongPassphraseAndMissingSignature()
    {
        var service = PayfastTestData.CreateService();

        var tampered = PayfastService.ParseFormBody(PayfastTestData.Sign(PayfastTestData.ItnFields()).Replace("amount_gross=150.00", "amount_gross=1.00"));
        Assert.False(service.IsValidItnSignature(tampered));

        Assert.False(service.IsValidItnSignature(PayfastService.ParseFormBody(PayfastTestData.Sign(PayfastTestData.ItnFields(), "other"))));
        Assert.False(service.IsValidItnSignature(PayfastTestData.ItnFields()));
        Assert.True(service.IsValidItnSignature(PayfastService.ParseFormBody(PayfastTestData.Sign(PayfastTestData.ItnFields()))));
    }

    [Fact]
    public void ItnSignature_IgnoresWhitespaceAroundConfiguredPassphrase()
    {
        // e.g. a Railway variable pasted with a trailing newline: checkout trims it, so the ITN must too.
        var settings = PayfastTestData.Settings();
        settings.Passphrase = $"  {PayfastTestData.Passphrase}\n";

        Assert.True(PayfastTestData.CreateService(settings: settings)
            .IsValidItnSignature(PayfastService.ParseFormBody(PayfastTestData.Sign(PayfastTestData.ItnFields()))));
    }

    [Theory]
    [InlineData("197.97.145.144", true)]
    [InlineData("197.97.145.159", true)]
    [InlineData("197.97.145.160", false)]
    [InlineData("41.74.179.223", true)]
    [InlineData("102.216.36.15", true)]
    [InlineData("102.216.36.128", true)]
    [InlineData("144.126.193.139", true)]
    [InlineData("144.126.193.140", false)]
    [InlineData("::ffff:197.97.145.150", true)]
    [InlineData("10.0.0.1", false)]
    public async Task SourceIp_DocumentedRanges(string ip, bool expected)
    {
        Assert.Equal(expected, await PayfastTestData.CreateService().IsValidSourceIpAsync(IPAddress.Parse(ip)));
    }

    [Fact]
    public async Task SourceIp_AlsoAcceptsAddressesOfPayfastHostnames_AndRejectsNull()
    {
        var service = PayfastTestData.CreateService(resolvedHostIps: new[] { PayfastTestData.OtherIp });

        Assert.True(await service.IsValidSourceIpAsync(PayfastTestData.OtherIp));
        Assert.False(await service.IsValidSourceIpAsync(null));
    }

    [Fact]
    public async Task Confirm_PostsParamStringWithoutPassphrase_ToValidateUrl()
    {
        var handler = FakeHttpMessageHandler.Returning(HttpStatusCode.OK, "VALID");
        var fields = PayfastService.ParseFormBody(PayfastTestData.Sign(PayfastTestData.ItnFields()));

        Assert.True(await PayfastTestData.CreateService(handler).ConfirmWithPayfastAsync(fields, PayfastTestData.OrderId));

        var (request, body) = Assert.Single(handler.Requests);
        Assert.Equal("https://sandbox.payfast.co.za/eng/query/validate", request.RequestUri!.ToString());
        Assert.Equal(PayfastService.BuildItnParamString(fields), body);
        Assert.DoesNotContain("passphrase", body);
        Assert.DoesNotContain("signature=", body);
    }

    [Fact]
    public async Task Confirm_InvalidReplyIsFalse_HttpErrorThrows()
    {
        var fields = PayfastTestData.ItnFields();

        Assert.False(await PayfastTestData.CreateService(FakeHttpMessageHandler.Returning(HttpStatusCode.OK, "INVALID")).ConfirmWithPayfastAsync(fields, null));
        await Assert.ThrowsAsync<PayfastApiException>(() =>
            PayfastTestData.CreateService(FakeHttpMessageHandler.Returning(HttpStatusCode.InternalServerError, "")).ConfirmWithPayfastAsync(fields, null));
    }
}

// SQLite in-memory: unlike the EF in-memory provider it executes ExecuteUpdate, so the
// atomic "claim Paid" path runs for real.
public sealed class SqliteTestDbContext : ApplicationDbContext
{
    private readonly SqliteConnection _connection;

    private SqliteTestDbContext(SqliteConnection connection)
        : base(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options)
    {
        _connection = connection;
    }

    public static SqliteTestDbContext Create()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var context = new SqliteTestDbContext(connection);
        context.Database.EnsureCreated();
        return context;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<KnowledgeChunk>().Ignore(e => e.Embedding);
    }

    public override void Dispose()
    {
        base.Dispose();
        _connection.Dispose();
    }
}

public class PayfastItnProcessorTests : IDisposable
{
    private readonly SqliteTestDbContext _context = SqliteTestDbContext.Create();
    private readonly FakeHttpMessageHandler _resendHttp = FakeHttpMessageHandler.Returning(HttpStatusCode.OK, "{}");

    public PayfastItnProcessorTests()
    {
        _context.Orders.Add(new Order
        {
            OrderId = PayfastTestData.OrderId,
            Amount = 150m,
            Currency = "ZAR",
            Status = "Pending",
            Name = "Zoë",
            Email = "zoe+test@example.test",
            CreatedAt = DateTime.UtcNow,
            PaymentProvider = PaymentProviderNames.Payfast
        });
        _context.SaveChanges();
        _context.ChangeTracker.Clear();
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task Complete_MarksPaid_SendsEmailsOnce_AndDuplicateIsIgnored()
    {
        var validate = FakeHttpMessageHandler.Returning(HttpStatusCode.OK, "VALID");
        var body = PayfastTestData.Sign(PayfastTestData.ItnFields());

        var first = await CreateProcessor(validate).ProcessAsync(body, PayfastTestData.PayfastIp);
        _context.ChangeTracker.Clear();
        var second = await CreateProcessor(validate).ProcessAsync(body, PayfastTestData.PayfastIp);

        Assert.Equal(PayfastItnOutcome.Processed, first.Outcome);
        Assert.Equal("Paid", first.OrderStatus);
        Assert.Equal(PayfastItnOutcome.Processed, second.Outcome);

        var order = await _context.Orders.AsNoTracking().SingleAsync();
        Assert.Equal("Paid", order.Status);
        Assert.Equal("1089250", order.PayfastPaymentId);
        Assert.NotNull(order.PaidAt);

        // Two emails (admin + customer) from the first ITN only; one validation call.
        Assert.Equal(2, _resendHttp.Requests.Count);
        Assert.Single(validate.Requests);
    }

    [Fact]
    public async Task ConcurrentDuplicate_OnlyOneClaimsPaid()
    {
        // Simulate a second ITN that already passed the "already Paid" check: the conditional
        // UPDATE must still refuse to claim the order twice.
        var body = PayfastTestData.Sign(PayfastTestData.ItnFields());
        await CreateProcessor().ProcessAsync(body, PayfastTestData.PayfastIp);

        var claimedAgain = await _context.Orders
            .Where(o => o.OrderId == PayfastTestData.OrderId && o.Status != "Paid")
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, "Paid"));

        Assert.Equal(0, claimedAgain);
    }

    [Fact]
    public async Task BadSignature_Rejected_NotPaid()
    {
        var body = PayfastTestData.Sign(PayfastTestData.ItnFields(), passphrase: "wrong");

        var result = await CreateProcessor().ProcessAsync(body, PayfastTestData.PayfastIp);

        Assert.Equal(PayfastItnOutcome.InvalidSignature, result.Outcome);
        await AssertOrderStatus("Pending");
    }

    [Fact]
    public async Task BadSourceIp_Rejected_NotPaid()
    {
        var result = await CreateProcessor().ProcessAsync(PayfastTestData.Sign(PayfastTestData.ItnFields()), PayfastTestData.OtherIp);

        Assert.Equal(PayfastItnOutcome.InvalidSource, result.Outcome);
        await AssertOrderStatus("Pending");
    }

    [Fact]
    public async Task WrongAmount_Rejected_NotPaid()
    {
        var result = await CreateProcessor().ProcessAsync(PayfastTestData.Sign(PayfastTestData.ItnFields(amountGross: "15.00")), PayfastTestData.PayfastIp);

        Assert.Equal(PayfastItnOutcome.AmountMismatch, result.Outcome);
        await AssertOrderStatus("Pending");
    }

    [Fact]
    public async Task OtherMerchant_Rejected_NotPaid()
    {
        var result = await CreateProcessor().ProcessAsync(PayfastTestData.Sign(PayfastTestData.ItnFields(merchantId: "22222222")), PayfastTestData.PayfastIp);

        Assert.Equal(PayfastItnOutcome.MerchantMismatch, result.Outcome);
        await AssertOrderStatus("Pending");
    }

    [Fact]
    public async Task PayfastSaysInvalid_Rejected_NotPaid()
    {
        var result = await CreateProcessor(FakeHttpMessageHandler.Returning(HttpStatusCode.OK, "INVALID"))
            .ProcessAsync(PayfastTestData.Sign(PayfastTestData.ItnFields()), PayfastTestData.PayfastIp);

        Assert.Equal(PayfastItnOutcome.ValidationRejected, result.Outcome);
        await AssertOrderStatus("Pending");
    }

    [Fact]
    public async Task ValidationUnreachable_ReportsUnavailable_SoPayfastRetries()
    {
        var result = await CreateProcessor(FakeHttpMessageHandler.Returning(HttpStatusCode.ServiceUnavailable, ""))
            .ProcessAsync(PayfastTestData.Sign(PayfastTestData.ItnFields()), PayfastTestData.PayfastIp);

        Assert.Equal(PayfastItnOutcome.ValidationUnavailable, result.Outcome);
        await AssertOrderStatus("Pending");
    }

    [Theory]
    [InlineData("FAILED", "Failed")]
    [InlineData("CANCELLED", "Cancelled")]
    [InlineData("PENDING", "Pending")]
    [InlineData("SOMETHING_NEW", "Pending")]
    public async Task NonCompleteStatuses(string paymentStatus, string expectedOrderStatus)
    {
        var result = await CreateProcessor().ProcessAsync(PayfastTestData.Sign(PayfastTestData.ItnFields(paymentStatus)), PayfastTestData.PayfastIp);

        Assert.Equal(PayfastItnOutcome.Processed, result.Outcome);
        await AssertOrderStatus(expectedOrderStatus);
        Assert.Empty(_resendHttp.Requests);
    }

    [Fact]
    public async Task FailedAfterPaid_DoesNotDowngrade()
    {
        await CreateProcessor().ProcessAsync(PayfastTestData.Sign(PayfastTestData.ItnFields()), PayfastTestData.PayfastIp);
        _context.ChangeTracker.Clear();

        // Already-Paid orders short-circuit; and the conditional UPDATE excludes Paid anyway.
        await CreateProcessor().ProcessAsync(PayfastTestData.Sign(PayfastTestData.ItnFields("FAILED")), PayfastTestData.PayfastIp);

        await AssertOrderStatus("Paid");
    }

    [Fact]
    public async Task UnknownOrder_Acknowledged()
    {
        var fields = PayfastTestData.ItnFields();
        fields[0] = new("m_payment_id", Guid.NewGuid().ToString());

        var result = await CreateProcessor().ProcessAsync(PayfastTestData.Sign(fields), PayfastTestData.PayfastIp);

        Assert.Equal(PayfastItnOutcome.UnknownOrder, result.Outcome);
    }

    [Fact]
    public async Task NotConfigured_ReportsNotConfigured()
    {
        var settings = PayfastTestData.Settings();
        settings.MerchantKey = "";

        var result = await CreateProcessor(settings: settings).ProcessAsync(PayfastTestData.Sign(PayfastTestData.ItnFields()), PayfastTestData.PayfastIp);

        Assert.Equal(PayfastItnOutcome.NotConfigured, result.Outcome);
    }

    [Fact]
    public async Task WebhookController_MapsOutcomesToStatusCodes()
    {
        Assert.IsType<OkResult>(await PostItn(PayfastTestData.Sign(PayfastTestData.ItnFields()), PayfastTestData.PayfastIp));
        Assert.Equal(403, Assert.IsType<StatusCodeResult>(await PostItn(PayfastTestData.Sign(PayfastTestData.ItnFields()), PayfastTestData.OtherIp)).StatusCode);
        Assert.IsType<BadRequestResult>(await PostItn(PayfastTestData.Sign(PayfastTestData.ItnFields(), "wrong"), PayfastTestData.PayfastIp));
    }

    private async Task<IActionResult> PostItn(string body, IPAddress ip)
    {
        _context.ChangeTracker.Clear();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(body));
        httpContext.Connection.RemoteIpAddress = ip;

        var controller = new PayfastWebhooksController(CreateProcessor(), NullLogger<PayfastWebhooksController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext }
        };

        return await controller.Receive(CancellationToken.None);
    }

    private async Task AssertOrderStatus(string expected)
    {
        var order = await _context.Orders.AsNoTracking().SingleAsync(o => o.OrderId == PayfastTestData.OrderId);
        Assert.Equal(expected, order.Status);
    }

    private PayfastItnProcessor CreateProcessor(FakeHttpMessageHandler? validate = null, PayfastSettings? settings = null) =>
        new(
            PayfastTestData.CreateService(validate, settings),
            _context,
            new ResendService(
                new HttpClient(_resendHttp) { BaseAddress = new Uri("https://api.resend.com/") },
                Options.Create(new ResendSettings { ToEmail = "admin@example.test" })),
            new ICloudCalendarService(new HttpClient(), new ICloudSettings(), _context, new ClientCredentialProtector(new EphemeralDataProtectionProvider())),
            NullLogger<PayfastItnProcessor>.Instance);
}

public class PayfastCheckoutEndToEndTests
{
    [Fact]
    public async Task RealGateway_CreatesPayfastOrder_AndSignedForm()
    {
        using var context = new TestDbContext();
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<ApplicationDbContext>(context)
            .AddSingleton<IPayfastService>(PayfastTestData.CreateService())
            .AddSingleton(new ResendService(
                new HttpClient(FakeHttpMessageHandler.Returning(HttpStatusCode.OK, "{}")) { BaseAddress = new Uri("https://api.resend.com/") },
                Options.Create(new ResendSettings())))
            .AddSingleton(new ICloudCalendarService(
                new HttpClient(), new ICloudSettings(), context, new ClientCredentialProtector(new EphemeralDataProtectionProvider())));
        var provider = services.BuildServiceProvider();

        var request = new CreateCheckoutRequest { Amount = 150m, Name = "Zoë", Surname = "Test", Email = "zoe@example.test", ClientApiKey = "client-key" };
        var result = await new CheckoutProviderGateway().CreatePayfastCheckoutAsync(
            request, new ControllerContext { HttpContext = new DefaultHttpContext { RequestServices = provider } });

        var response = Assert.IsType<TheAdamsParadigm.Api.Models.Payfast.PayfastCheckoutResponse>(Assert.IsType<OkObjectResult>(result).Value);
        var order = await context.Orders.AsNoTracking().SingleAsync();
        Assert.Equal("Payfast", order.PaymentProvider);
        Assert.Equal("Pending", order.Status);
        Assert.Equal(order.OrderId, response.Fields.Single(f => f.Name == "m_payment_id").Value);
        Assert.Equal("signature", response.Fields.Last().Name);
        Assert.Equal("https://sandbox.payfast.co.za/eng/process", response.ProcessUrl);
    }

    [Fact]
    public async Task RealController_BelowMinimum_Returns400_AndCreatesNoOrder()
    {
        using var context = new TestDbContext();
        var controller = new PayfastPaymentsController(
            PayfastTestData.CreateService(), context,
            new ResendService(new HttpClient(), Options.Create(new ResendSettings())),
            new ICloudCalendarService(new HttpClient(), new ICloudSettings(), context, new ClientCredentialProtector(new EphemeralDataProtectionProvider())),
            NullLogger<PayfastPaymentsController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.CreateCheckout(new CreateCheckoutRequest { Amount = 4m, ClientApiKey = "k" }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(context.Orders);
    }
}

public class PayfastProviderSwitchTests
{
    [Fact]
    public async Task DefaultProvider_IsPayfast_WhenConfigured()
    {
        using var context = new TestDbContext();

        Assert.Equal("Payfast", await PaymentProviderSettingServiceTests.CreateService(context, defaultProvider: "payfast").GetActiveProviderAsync());
    }

    [Fact]
    public async Task Switch_ToPayfast_WithMissingConfig_IsRefused()
    {
        using var context = new TestDbContext();
        var service = PaymentProviderSettingServiceTests.CreateService(context, payfast: new PayfastSettings());

        var ex = await Assert.ThrowsAsync<PaymentProviderNotConfiguredException>(() => service.SetActiveProviderAsync("Payfast"));

        Assert.Contains("Payfast__MerchantId", ex.Missing);
        Assert.Contains("Payfast__Passphrase", ex.Missing);
        Assert.Contains("Payfast__NotifyUrl", ex.Missing);
        Assert.Empty(context.AppSettings);
    }

    [Fact]
    public async Task Switch_ToPayfast_WhenConfigured_Works()
    {
        using var context = new TestDbContext();
        var service = PaymentProviderSettingServiceTests.CreateService(context);

        var (_, newProvider) = await service.SetActiveProviderAsync("PAYFAST");

        Assert.Equal("Payfast", newProvider);
    }

    [Fact]
    public async Task Dispatcher_RoutesToPayfast_AndReturnsFormAction()
    {
        using var context = new TestDbContext();
        var gateway = new PaymentProviderControllerTests.FakeGateway();
        var controller = new PaymentProviderController(
            PaymentProviderSettingServiceTests.CreateService(context, defaultProvider: "Payfast"),
            gateway,
            NullLogger<PaymentProviderController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = Assert.IsType<OkObjectResult>(await controller.Checkout(new CreateCheckoutRequest(), CancellationToken.None));

        Assert.Equal(new[] { "Payfast" }, gateway.Calls);
        var response = Assert.IsType<CheckoutResponse>(result.Value);
        Assert.Equal("Payfast", response.Provider);
        Assert.Equal(CheckoutActions.Form, response.Action);
        Assert.Equal("https://sandbox.payfast.co.za/eng/process", response.ProcessUrl);
        Assert.Null(response.PaymentUrl);
        Assert.Equal("pf-order", response.OrderNumber);
        Assert.NotNull(response.Fields);
    }

    [Fact]
    public async Task Dispatcher_YocoAndPaystack_StillRedirect()
    {
        using var context = new TestDbContext();
        var service = PaymentProviderSettingServiceTests.CreateService(context);
        var controller = new PaymentProviderController(service, new PaymentProviderControllerTests.FakeGateway(), NullLogger<PaymentProviderController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var yoco = (CheckoutResponse)Assert.IsType<OkObjectResult>(await controller.Checkout(new CreateCheckoutRequest(), CancellationToken.None)).Value!;
        await service.SetActiveProviderAsync("Paystack");
        var paystack = (CheckoutResponse)Assert.IsType<OkObjectResult>(await controller.Checkout(new CreateCheckoutRequest(), CancellationToken.None)).Value!;

        Assert.Equal(CheckoutActions.Redirect, yoco.Action);
        Assert.Equal(CheckoutActions.Redirect, paystack.Action);
        Assert.Null(yoco.Fields);
    }
}
