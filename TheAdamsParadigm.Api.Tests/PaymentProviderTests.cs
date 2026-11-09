using System.Net;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TheAdamsParadigm.Api.Configuration;
using TheAdamsParadigm.Api.Controllers;
using TheAdamsParadigm.Api.Data;
using TheAdamsParadigm.Api.Filters;
using TheAdamsParadigm.Api.Models;
using TheAdamsParadigm.Api.Models.Payfast;
using TheAdamsParadigm.Api.Models.Payments;
using TheAdamsParadigm.Api.Models.Paystack;
using TheAdamsParadigm.Api.Services;
using TheAdamsParadigm.Api.Services.CloudCalendarService;
using TheAdamsParadigm.Api.Services.Payments;
using TheAdamsParadigm.Api.Services.Paystack;
using Xunit;

namespace TheAdamsParadigm.Api.Tests;

public class PaymentProviderSettingServiceTests
{
    [Fact]
    public async Task Default_IsYoco_WhenNothingIsSet()
    {
        using var context = new TestDbContext();

        Assert.Equal("Yoco", await CreateService(context).GetActiveProviderAsync());
    }

    [Fact]
    public async Task Default_UsesConfiguredDefaultProvider_WhenNoRow()
    {
        using var context = new TestDbContext();

        Assert.Equal("Paystack", await CreateService(context, defaultProvider: "paystack").GetActiveProviderAsync());
    }

    [Fact]
    public async Task Default_FallsBackToYoco_WhenConfiguredDefaultIsInvalid()
    {
        using var context = new TestDbContext();

        Assert.Equal("Yoco", await CreateService(context, defaultProvider: "Stripe").GetActiveProviderAsync());
    }

    [Theory]
    [InlineData("Paystack", "Paystack")]
    [InlineData("paystack", "Paystack")]
    [InlineData("  PAYSTACK ", "Paystack")]
    [InlineData("yoco", "Yoco")]
    public async Task Switch_NormalisesAndPersists(string requested, string expected)
    {
        using var context = new TestDbContext();
        var service = CreateService(context);

        var (oldProvider, newProvider) = await service.SetActiveProviderAsync(requested);

        Assert.Equal("Yoco", oldProvider);
        Assert.Equal(expected, newProvider);
        Assert.Equal(expected, await service.GetActiveProviderAsync());
        var row = await context.AppSettings.AsNoTracking().SingleAsync();
        Assert.Equal(PaymentProviderSettingService.SettingKey, row.Key);
        Assert.Equal(expected, row.Value);
    }

    [Fact]
    public async Task Switch_BackAndForth_UpdatesSingleRow()
    {
        using var context = new TestDbContext();
        var service = CreateService(context);

        await service.SetActiveProviderAsync("Paystack");
        var (oldProvider, newProvider) = await service.SetActiveProviderAsync("Yoco");

        Assert.Equal("Paystack", oldProvider);
        Assert.Equal("Yoco", newProvider);
        Assert.Equal(1, await context.AppSettings.CountAsync());
    }

    [Theory]
    [InlineData("Stripe")]
    [InlineData("")]
    [InlineData(null)]
    public async Task Switch_RejectsInvalidValues(string? requested)
    {
        using var context = new TestDbContext();

        var ex = await Assert.ThrowsAsync<InvalidPaymentProviderException>(
            () => CreateService(context).SetActiveProviderAsync(requested));

        Assert.Contains("Yoco, Paystack", ex.Message);
        Assert.Empty(context.AppSettings);
    }

    [Fact]
    public async Task Switch_ToPaystackWithMissingConfig_IsRejectedListingWhatIsMissing()
    {
        using var context = new TestDbContext();
        var service = CreateService(context, paystackSecret: "", paystackCallback: "");

        var ex = await Assert.ThrowsAsync<PaymentProviderNotConfiguredException>(() => service.SetActiveProviderAsync("Paystack"));

        Assert.Equal(new[] { "Paystack__SecretKey", "Paystack__CallbackUrl" }, ex.Missing);
        Assert.Empty(context.AppSettings);
    }

    [Fact]
    public async Task Switch_ToYocoWithMissingSecret_IsRejected_AndNoSecretsInMessage()
    {
        using var context = new TestDbContext();
        var service = CreateService(context, yocoSecret: "");

        var ex = await Assert.ThrowsAsync<PaymentProviderNotConfiguredException>(() => service.SetActiveProviderAsync("Yoco"));

        Assert.Equal(new[] { "Yoco__SecretKey" }, ex.Missing);
        Assert.DoesNotContain("sk_test", ex.Message);
    }

    internal static PaymentProviderSettingService CreateService(
        ApplicationDbContext context,
        string defaultProvider = "Yoco",
        string yocoSecret = "sk_test_yoco",
        string paystackSecret = "sk_test_paystack",
        string paystackCallback = "https://example.test/payment/paystack-callback",
        PayfastSettings? payfast = null) =>
        new(
            context,
            Options.Create(new PaymentsSettings { DefaultProvider = defaultProvider }),
            Options.Create(new YocoSettings { SecretKey = yocoSecret }),
            Options.Create(new PaystackSettings { SecretKey = paystackSecret, CallbackUrl = paystackCallback }),
            Options.Create(payfast ?? PayfastTestData.Settings()),
            NullLogger<PaymentProviderSettingService>.Instance);
}

public class RequireAdminKeyAttributeTests
{
    private const string AdminKey = "test-admin-key-123";

    [Fact]
    public void MissingHeader_Returns401()
    {
        var context = CreateContext(AdminKey, providedKey: null);

        new RequireAdminKeyAttribute().OnAuthorization(context);

        Assert.IsType<UnauthorizedObjectResult>(context.Result);
    }

    [Theory]
    [InlineData("wrong")]
    [InlineData("test-admin-key-12")]
    [InlineData("TEST-ADMIN-KEY-123")]
    public void WrongKey_Returns401(string providedKey)
    {
        var context = CreateContext(AdminKey, providedKey);

        new RequireAdminKeyAttribute().OnAuthorization(context);

        Assert.IsType<UnauthorizedObjectResult>(context.Result);
    }

    [Fact]
    public void AdminKeyNotConfigured_Returns503_EvenWithAHeader()
    {
        var context = CreateContext(configuredKey: "", providedKey: "anything");

        new RequireAdminKeyAttribute().OnAuthorization(context);

        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(503, result.StatusCode);
    }

    [Fact]
    public void CorrectKey_Passes()
    {
        var context = CreateContext(AdminKey, AdminKey);

        new RequireAdminKeyAttribute().OnAuthorization(context);

        Assert.Null(context.Result);
    }

    private static AuthorizationFilterContext CreateContext(string configuredKey, string? providedKey)
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IOptions<AdminSettings>>(Options.Create(new AdminSettings { ApiKey = configuredKey }))
            .BuildServiceProvider();

        var httpContext = new DefaultHttpContext { RequestServices = services };
        httpContext.Request.Method = "PUT";
        httpContext.Request.Path = "/api/payments/provider";
        if (providedKey != null)
        {
            httpContext.Request.Headers[RequireAdminKeyAttribute.HeaderName] = providedKey;
        }

        return new AuthorizationFilterContext(
            new ActionContext(httpContext, new RouteData(), new ActionDescriptor()),
            new List<IFilterMetadata>());
    }
}

public class PaymentProviderControllerTests
{
    [Fact]
    public async Task GetProvider_ReturnsActiveProvider_WithNoStore()
    {
        using var context = new TestDbContext();
        var controller = CreateController(context, new FakeGateway());

        var result = Assert.IsType<OkObjectResult>(await controller.GetProvider(CancellationToken.None));

        Assert.Equal("Yoco", Assert.IsType<PaymentProviderResponse>(result.Value).Provider);
        Assert.Equal("no-store", controller.Response.Headers.CacheControl.ToString());
    }

    [Fact]
    public async Task SetProvider_ReturnsNewValue()
    {
        using var context = new TestDbContext();
        var controller = CreateController(context, new FakeGateway());

        var result = Assert.IsType<OkObjectResult>(
            await controller.SetProvider(new SetPaymentProviderRequest { Provider = "paystack" }, CancellationToken.None));

        Assert.Equal("Paystack", Assert.IsType<PaymentProviderResponse>(result.Value).Provider);
    }

    [Fact]
    public async Task SetProvider_Invalid_Returns400()
    {
        using var context = new TestDbContext();
        var controller = CreateController(context, new FakeGateway());

        var result = await controller.SetProvider(new SetPaymentProviderRequest { Provider = "Stripe" }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task SetProvider_MissingConfig_Returns400()
    {
        using var context = new TestDbContext();
        var service = PaymentProviderSettingServiceTests.CreateService(context, paystackSecret: "");
        var controller = CreateController(context, new FakeGateway(), service);

        var result = await controller.SetProvider(new SetPaymentProviderRequest { Provider = "Paystack" }, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Yoco", await service.GetActiveProviderAsync());
    }

    [Fact]
    public async Task Checkout_WhenYocoActive_CallsYocoOnly()
    {
        using var context = new TestDbContext();
        var gateway = new FakeGateway();

        var result = Assert.IsType<OkObjectResult>(
            await CreateController(context, gateway).Checkout(new CreateCheckoutRequest(), CancellationToken.None));

        Assert.Equal(new[] { "Yoco" }, gateway.Calls);
        var response = Assert.IsType<CheckoutResponse>(result.Value);
        Assert.Equal("Yoco", response.Provider);
        Assert.Equal("yoco-order", response.OrderId);
        Assert.Equal("yoco-order", response.OrderNumber);
        Assert.Equal("https://c.yoco.com/checkout/ch_1", response.PaymentUrl);
        Assert.Equal(150m, response.Amount);
        Assert.Equal("ZAR", response.Currency);
    }

    [Fact]
    public async Task Checkout_WhenPaystackActive_CallsPaystackOnly()
    {
        using var context = new TestDbContext();
        var service = PaymentProviderSettingServiceTests.CreateService(context);
        await service.SetActiveProviderAsync("Paystack");
        var gateway = new FakeGateway();

        var result = Assert.IsType<OkObjectResult>(
            await CreateController(context, gateway, service).Checkout(new CreateCheckoutRequest(), CancellationToken.None));

        Assert.Equal(new[] { "Paystack" }, gateway.Calls);
        var response = Assert.IsType<CheckoutResponse>(result.Value);
        Assert.Equal("Paystack", response.Provider);
        Assert.Equal("ps-order", response.OrderNumber);
        Assert.Equal("https://checkout.paystack.com/abc", response.PaymentUrl);
    }

    [Fact]
    public async Task Checkout_PassesProviderErrorsThroughUnchanged()
    {
        using var context = new TestDbContext();
        var error = new ObjectResult(new { error = "Yoco API error" }) { StatusCode = 502 };
        var gateway = new FakeGateway { YocoResult = error };

        var result = await CreateController(context, gateway).Checkout(new CreateCheckoutRequest(), CancellationToken.None);

        Assert.Same(error, result);
    }

    private static PaymentProviderController CreateController(
        ApplicationDbContext context,
        ICheckoutProviderGateway gateway,
        PaymentProviderSettingService? service = null) =>
        new(
            service ?? PaymentProviderSettingServiceTests.CreateService(context),
            gateway,
            NullLogger<PaymentProviderController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

    internal class FakeGateway : ICheckoutProviderGateway
    {
        public List<string> Calls { get; } = new();

        public IActionResult YocoResult { get; set; } = new OkObjectResult(new CreateCheckoutResponse
        {
            OrderId = "yoco-order",
            CheckoutId = "ch_1",
            PaymentUrl = "https://c.yoco.com/checkout/ch_1",
            Amount = 150m,
            Currency = "ZAR"
        });

        public Task<IActionResult> CreateYocoCheckoutAsync(CreateCheckoutRequest request, ControllerContext controllerContext)
        {
            Calls.Add("Yoco");
            return Task.FromResult(YocoResult);
        }

        public Task<IActionResult> CreatePaystackCheckoutAsync(CreateCheckoutRequest request, ControllerContext controllerContext)
        {
            Calls.Add("Paystack");
            return Task.FromResult<IActionResult>(new OkObjectResult(new PaystackCheckoutResponse
            {
                OrderId = "ps-order",
                OrderNumber = "ps-order",
                Reference = "TAP-ps-order",
                PaymentUrl = "https://checkout.paystack.com/abc",
                Amount = 150m,
                Currency = "ZAR"
            }));
        }

        public Task<IActionResult> CreatePayfastCheckoutAsync(CreateCheckoutRequest request, ControllerContext controllerContext)
        {
            Calls.Add("Payfast");
            return Task.FromResult<IActionResult>(new OkObjectResult(new PayfastCheckoutResponse
            {
                OrderId = "pf-order",
                OrderNumber = "pf-order",
                ProcessUrl = "https://sandbox.payfast.co.za/eng/process",
                Fields = new[] { new PayfastFormField("m_payment_id", "pf-order"), new PayfastFormField("signature", "abc") },
                Amount = 150m,
                Currency = "ZAR"
            }));
        }
    }
}

// The real gateway, driving the real (unmodified) Yoco and Paystack controllers through DI
// with fake HTTP for the providers and Resend.
public class CheckoutProviderGatewayTests
{
    [Fact]
    public async Task Yoco_CreatesOrderThroughExistingController()
    {
        using var context = new TestDbContext();
        var services = BuildServices(context);

        var result = await new CheckoutProviderGateway().CreateYocoCheckoutAsync(ValidRequest(), ControllerContextFor(services));

        var response = Assert.IsType<CreateCheckoutResponse>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal("https://c.yoco.com/checkout/ch_1", response.PaymentUrl);
        var order = await context.Orders.AsNoTracking().SingleAsync();
        Assert.Equal("Yoco", order.PaymentProvider);
        Assert.Equal("ch_1", order.CheckoutId);
    }

    [Fact]
    public async Task Paystack_CreatesOrderThroughExistingController()
    {
        using var context = new TestDbContext();
        var services = BuildServices(context);

        var result = await new CheckoutProviderGateway().CreatePaystackCheckoutAsync(ValidRequest(), ControllerContextFor(services));

        var response = Assert.IsType<PaystackCheckoutResponse>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal("https://checkout.paystack.com/abc", response.PaymentUrl);
        var order = await context.Orders.AsNoTracking().SingleAsync();
        Assert.Equal("Paystack", order.PaymentProvider);
        Assert.Equal($"TAP-{order.OrderId}", order.PaystackReference);
    }

    [Fact]
    public async Task Yoco_ValidationErrorFromExistingController_IsReturnedAsIs()
    {
        using var context = new TestDbContext();
        var services = BuildServices(context);
        var request = ValidRequest();
        request.Amount = 0;

        var result = await new CheckoutProviderGateway().CreateYocoCheckoutAsync(request, ControllerContextFor(services));

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(context.Orders);
    }

    private static CreateCheckoutRequest ValidRequest() => new()
    {
        Amount = 150m,
        Name = "Test",
        Surname = "Customer",
        Email = "customer@example.test",
        ClientApiKey = "client-key"
    };

    private static ControllerContext ControllerContextFor(IServiceProvider services) =>
        new() { HttpContext = new DefaultHttpContext { RequestServices = services } };

    private static IServiceProvider BuildServices(ApplicationDbContext context)
    {
        var yocoHttp = FakeHttpMessageHandler.Returning(HttpStatusCode.OK,
            """{"id":"ch_1","redirectUrl":"https://c.yoco.com/checkout/ch_1","status":"created"}""");
        var paystackHttp = FakeHttpMessageHandler.Returning(HttpStatusCode.OK,
            """{"status":true,"data":{"authorization_url":"https://checkout.paystack.com/abc","access_code":"abc","reference":"TAP-from-paystack"}}""");

        return new ServiceCollection()
            .AddLogging()
            .AddSingleton(context)
            .AddSingleton(new YocoService(
                new HttpClient(yocoHttp) { BaseAddress = new Uri("https://payments.yoco.com/") },
                Options.Create(new YocoSettings { SecretKey = "sk_test_yoco" }),
                NullLogger<YocoService>.Instance))
            .AddSingleton<IPaystackService>(new PaystackService(
                new HttpClient(paystackHttp) { BaseAddress = new Uri("https://api.paystack.co/") },
                Options.Create(new PaystackSettings { SecretKey = "sk_test_paystack", CallbackUrl = "https://example.test/cb" }),
                NullLogger<PaystackService>.Instance))
            .AddSingleton(new ResendService(
                new HttpClient(FakeHttpMessageHandler.Returning(HttpStatusCode.OK, "{}")) { BaseAddress = new Uri("https://api.resend.com/") },
                Options.Create(new ResendSettings())))
            .AddSingleton(new ICloudCalendarService(
                new HttpClient(),
                new ICloudSettings(),
                context,
                new ClientCredentialProtector(new EphemeralDataProtectionProvider())))
            .AddSingleton<PaystackPaymentProcessor>()
            .BuildServiceProvider();
    }
}
