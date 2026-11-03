using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using TheAdamsParadigm.Api.Configuration;

namespace TheAdamsParadigm.Api.Filters;

// Protects an endpoint with the shared admin key: callers send it in the X-Admin-Key
// header, compared (as SHA-256 hashes, in constant time) with Admin__ApiKey.
// 503 when Admin__ApiKey isn't configured (admin endpoints disabled), 401 when the
// header is missing or wrong. The key itself is never logged.
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireAdminKeyAttribute : Attribute, IAuthorizationFilter
{
    public const string HeaderName = "X-Admin-Key";

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var services = context.HttpContext.RequestServices;
        var settings = services.GetRequiredService<IOptions<AdminSettings>>().Value;
        var logger = services.GetRequiredService<ILogger<RequireAdminKeyAttribute>>();
        var endpoint = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";

        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            logger.LogWarning("Admin endpoint {Endpoint} called but Admin__ApiKey is not configured — endpoint disabled.", endpoint);
            context.Result = new ObjectResult(new { error = "This admin endpoint is disabled." })
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable
            };
            return;
        }

        var provided = context.HttpContext.Request.Headers[HeaderName].ToString();

        if (string.IsNullOrEmpty(provided) || !KeysMatch(provided, settings.ApiKey))
        {
            logger.LogWarning(
                "Admin endpoint {Endpoint} rejected: {Reason}. Remote IP: {RemoteIp}",
                endpoint,
                string.IsNullOrEmpty(provided) ? $"missing {HeaderName} header" : $"invalid {HeaderName}",
                context.HttpContext.Connection.RemoteIpAddress);
            context.Result = new UnauthorizedObjectResult(new { error = "Invalid or missing admin key." });
        }
    }

    // Hashing first gives both sides a fixed length, so FixedTimeEquals doesn't leak the key length.
    private static bool KeysMatch(string provided, string expected) =>
        CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(provided)),
            SHA256.HashData(Encoding.UTF8.GetBytes(expected)));
}
