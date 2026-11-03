using Microsoft.AspNetCore.Mvc;
using TheAdamsParadigm.Api.Controllers;
using TheAdamsParadigm.Api.Models;

namespace TheAdamsParadigm.Api.Services.Payments;

// Runs the existing provider-specific create-checkout actions so the dispatcher reuses
// them exactly as-is (validation, order creation, emails, error responses) without
// duplicating or modifying them. Behind an interface so the dispatcher can be tested
// with fakes.
public interface ICheckoutProviderGateway
{
    Task<IActionResult> CreateYocoCheckoutAsync(CreateCheckoutRequest request, ControllerContext controllerContext);

    Task<IActionResult> CreatePaystackCheckoutAsync(CreateCheckoutRequest request, ControllerContext controllerContext);
}

public class CheckoutProviderGateway : ICheckoutProviderGateway
{
    public Task<IActionResult> CreateYocoCheckoutAsync(CreateCheckoutRequest request, ControllerContext controllerContext)
    {
        var controller = ActivatorUtilities.CreateInstance<PaymentsController>(controllerContext.HttpContext.RequestServices);
        controller.ControllerContext = controllerContext;
        return controller.CreateCheckout(request);
    }

    public Task<IActionResult> CreatePaystackCheckoutAsync(CreateCheckoutRequest request, ControllerContext controllerContext)
    {
        var controller = ActivatorUtilities.CreateInstance<PaystackPaymentsController>(controllerContext.HttpContext.RequestServices);
        controller.ControllerContext = controllerContext;
        return controller.CreateCheckout(request, controllerContext.HttpContext.RequestAborted);
    }
}
