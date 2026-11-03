using System.Reflection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace TheAdamsParadigm.Api.Filters;

// Marks [RequireAdminKey] endpoints in Swagger so the UI sends the X-Admin-Key header for them.
public class AdminKeyOperationFilter : IOperationFilter
{
    public const string SchemeId = "AdminKey";

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var requiresAdminKey =
            context.MethodInfo.GetCustomAttribute<RequireAdminKeyAttribute>() != null ||
            context.MethodInfo.DeclaringType?.GetCustomAttribute<RequireAdminKeyAttribute>() != null;

        if (!requiresAdminKey)
        {
            return;
        }

        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = SchemeId }
            }] = Array.Empty<string>()
        });
    }
}
