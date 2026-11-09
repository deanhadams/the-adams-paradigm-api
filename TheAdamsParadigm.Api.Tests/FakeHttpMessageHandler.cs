using System.Net;

namespace TheAdamsParadigm.Api.Tests;

// Records every outgoing request (with its body read eagerly) and replies with a canned response.
public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _respond;

    public List<(HttpRequestMessage Request, string? Body)> Requests { get; } = new();

    public FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        _respond = respond;
    }

    public static FakeHttpMessageHandler Returning(HttpStatusCode statusCode, string body) =>
        new(_ => new HttpResponseMessage(statusCode) { Content = new StringContent(body) });

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        Requests.Add((request, body));
        return _respond(request);
    }
}
