using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace TheAdamsParadigm.Api.Filters;

// Stops MVC from reading a form-encoded body into its form value provider before the action
// runs, so the action can read the raw body itself (needed for Payfast ITN signature checks,
// which depend on the exact posted field order).
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class DisableFormValueModelBindingAttribute : Attribute, IResourceFilter
{
    public void OnResourceExecuting(ResourceExecutingContext context)
    {
        context.ValueProviderFactories.RemoveType<FormValueProviderFactory>();
        context.ValueProviderFactories.RemoveType<FormFileValueProviderFactory>();
        context.ValueProviderFactories.RemoveType<JQueryFormValueProviderFactory>();
    }

    public void OnResourceExecuted(ResourceExecutedContext context)
    {
    }
}
