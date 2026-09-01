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

        await SendEmailAsync(_settings.ToEmail, subject, html, replyTo: request.Email);
    }

    public async Task SendBookingConfirmationAsync(Order order, string? serviceTitle)
    {
        var amountLine = $"{order.Amount:F2} {order.Currency}";

        var adminHtml = $"""
            <p>New booking received.</p>
            <p><strong>Order ID:</strong> {HtmlEncode(order.OrderId)}</p>
            <p><strong>Customer:</strong> {HtmlEncode(order.Name)} {HtmlEncode(order.Surname)} ({HtmlEncode(order.Email)})</p>
            <p><strong>Service:</strong> {HtmlEncode(serviceTitle ?? "Not specified")}</p>
            <p><strong>Amount:</strong> {amountLine}</p>
            """;

        await SendEmailAsync(_settings.ToEmail, $"New Booking — {order.OrderId}", adminHtml);

        if (!string.IsNullOrWhiteSpace(order.Email))
        {
            var customerHtml = $"""
                <p>Hi {HtmlEncode(order.Name)},</p>
                <p>Thanks for booking with The Adams Paradigm! Here's a summary of your order:</p>
                <p><strong>Order ID:</strong> {HtmlEncode(order.OrderId)}</p>
                <p><strong>Service:</strong> {HtmlEncode(serviceTitle ?? "Not specified")}</p>
                <p><strong>Amount:</strong> {amountLine}</p>
                <p>We'll send you another email once your payment has been confirmed.</p>
                """;

            await SendEmailAsync(order.Email, "Booking Confirmation — The Adams Paradigm", customerHtml);
        }
    }

    public async Task SendPaymentSuccessAsync(Order order)
    {
        var amountLine = $"{order.Amount:F2} {order.Currency}";

        var adminHtml = $"""
            <p>Payment received.</p>
            <p><strong>Order ID:</strong> {HtmlEncode(order.OrderId)}</p>
            <p><strong>Customer:</strong> {HtmlEncode(order.Name)} {HtmlEncode(order.Surname)} ({HtmlEncode(order.Email)})</p>
            <p><strong>Amount:</strong> {amountLine}</p>
            """;

        await SendEmailAsync(_settings.ToEmail, $"Payment Received — {order.OrderId}", adminHtml);

        if (!string.IsNullOrWhiteSpace(order.Email))
        {
            var customerHtml = $"""
                <p>Hi {HtmlEncode(order.Name)},</p>
                <p>We've received your payment for order <strong>{HtmlEncode(order.OrderId)}</strong>.</p>
                <p><strong>Amount:</strong> {amountLine}</p>
                <p>Thank you for your business!</p>
                """;

            await SendEmailAsync(order.Email, "Payment Confirmed — The Adams Paradigm", customerHtml);
        }
    }

    private async Task SendEmailAsync(string to, string subject, string html, string? replyTo = null)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "emails");
        httpRequest.Content = JsonContent.Create(new
        {
            from = _settings.FromEmail,
            to = new[] { to },
            reply_to = replyTo,
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
