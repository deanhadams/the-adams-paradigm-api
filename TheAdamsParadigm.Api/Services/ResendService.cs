using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Options;
using TheAdamsParadigm.Api.Configuration;
using TheAdamsParadigm.Api.Models;

namespace TheAdamsParadigm.Api.Services;

public class ResendService
{
    private readonly HttpClient _httpClient;
    private readonly ResendSettings _settings;

    public ResendService(HttpClient httpClient, IOptions<ResendSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;

        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
    }

    public async Task SendContactMessageAsync(ContactRequest request)
    {
        if (string.IsNullOrWhiteSpace(_settings.ToEmail))
        {
            throw new InvalidOperationException("Resend ToEmail is not configured.");
        }

        var subject = $"{(string.IsNullOrWhiteSpace(request.ContextLabel) ? "New project inquiry" : request.ContextLabel)} — {request.Name}";

        var html = $"""
            <p><strong>Name:</strong> {HtmlEncode(request.Name)}</p>
            <p><strong>Email:</strong> {HtmlEncode(request.Email)}</p>
            <p><strong>Project type:</strong> {HtmlEncode(request.ProjectType)}</p>
            <p><strong>Budget:</strong> {HtmlEncode(string.IsNullOrWhiteSpace(request.Budget) ? "Not specified" : request.Budget)}</p>
            <p><strong>Message:</strong></p>
            <p>{HtmlEncode(request.Message).Replace("\n", "<br/>")}</p>
            """;

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "emails");
        httpRequest.Content = JsonContent.Create(new
        {
            from = _settings.FromEmail,
            to = new[] { _settings.ToEmail },
            reply_to = request.Email,
            subject,
            html
        });

        var response = await _httpClient.SendAsync(httpRequest);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Resend returned {(int)response.StatusCode}: {responseBody}");
        }
    }

    private static string HtmlEncode(string value) => HtmlEncoder.Default.Encode(value);
}
