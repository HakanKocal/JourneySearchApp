using System.Net;
using System.Text;

namespace Obilet.Tests.Fakes;

/// <summary>
/// Sabit bir HTTP yanıtı döndüren mesaj işleyici.
/// </summary>
/// <remarks>
/// API istemcisinin yanıt yorumlamasını gerçek ağa çıkmadan test etmek için.
/// </remarks>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string _body;

    public StubHttpMessageHandler(HttpStatusCode statusCode, string body)
    {
        _statusCode = statusCode;
        _body = body;
    }

    /// <summary>Son gönderilen istek gövdesi. İstek şeklini doğrulamak için.</summary>
    public string? LastRequestBody { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.Content is not null)
        {
            LastRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
        }

        return new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_body, Encoding.UTF8, "application/json"),
        };
    }
}
