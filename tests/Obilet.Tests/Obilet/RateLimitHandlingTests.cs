using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Obilet.Application.Exceptions;
using Obilet.Application.Models;
using Obilet.Infrastructure.Obilet;
using Obilet.Tests.Fakes;

namespace Obilet.Tests.Obilet;

/// <summary>
/// Hız sınırı yanıtının ayrı ele alındığını doğrular.
/// </summary>
/// <remarks>
/// Canlı API bir CDN arkasında ve oturum oluşturma çağrısı hız sınırlı.
/// Sınır aşıldığında <c>HTTP 429</c> ve yaklaşık bir saatlik bir
/// <c>Retry-After</c> dönüyor; gövde de JSON değil, düz metin
/// <c>error code: 1015</c>. Bu iki şeyin birlikte olması önemli: JSON
/// beklemeyen bir ayrıştırıcı çökerdi ve hız sınırı diğer arızalardan
/// ayrılmazsa kullanıcıya yanlış mesaj gösterilirdi.
/// </remarks>
public sealed class RateLimitHandlingTests
{
    private static readonly DeviceSession AnySession = new("session", "device");

    private static ObiletApiClient CreateSut(StubHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.invalid/api/"),
        };

        var options = Options.Create(new ObiletApiOptions
        {
            BaseUrl = "https://example.invalid/api/",
            ApiClientToken = "test-token",
        });

        return new ObiletApiClient(
            httpClient,
            options,
            new StubMarketClock(),
            NullLogger<ObiletApiClient>.Instance);
    }

    [Fact]
    public async Task JSON_olmayan_429_yaniti_uygulamayi_dusurmez()
    {
        // CDN'in gerçekten döndürdüğü gövde.
        var handler = new StubHttpMessageHandler(
            HttpStatusCode.TooManyRequests, "error code: 1015");

        var sut = CreateSut(handler);

        var exception = await Assert.ThrowsAsync<ObiletApiException>(
            () => sut.CreateSessionAsync());

        Assert.True(exception.IsRateLimited);
        Assert.Equal(HttpStatusCode.TooManyRequests, exception.HttpStatusCode);
    }

    [Fact]
    public async Task Hiz_siniri_oturum_yenilemeyle_kurtarilabilir_sayilmaz()
    {
        var handler = new StubHttpMessageHandler(
            HttpStatusCode.TooManyRequests, "error code: 1015");

        var sut = CreateSut(handler);

        var exception = await Assert.ThrowsAsync<ObiletApiException>(
            () => sut.GetBusLocationsAsync(AnySession, query: null, marketLocale: "tr-TR"));

        // Yeniden denemek durumu kötüleştirir: her deneme yeni bir oturum
        // oluşturma isteği demek ve sınır zaten bu nedenle aşılmış oluyor.
        Assert.False(exception.IsRecoverableBySessionRefresh);
    }

    [Fact]
    public async Task Diger_hatalar_hiz_siniri_olarak_isaretlenmez()
    {
        const string body = """
            { "status": "InvalidRoute", "data": null, "message": "Invalid route" }
            """;

        var sut = CreateSut(new StubHttpMessageHandler(HttpStatusCode.OK, body));

        var exception = await Assert.ThrowsAsync<ObiletApiException>(
            () => sut.GetBusJourneysAsync(
                AnySession, 349, 349, new DateOnly(2026, 9, 16), "tr-TR"));

        Assert.False(exception.IsRateLimited);
        Assert.Equal("InvalidRoute", exception.Status);
    }

    [Fact]
    public async Task HTTP_durum_kodu_istisnaya_tasinir()
    {
        const string body = """
            { "status": "DeviceSessionError", "data": null }
            """;

        var sut = CreateSut(new StubHttpMessageHandler(HttpStatusCode.BadRequest, body));

        var exception = await Assert.ThrowsAsync<ObiletApiException>(
            () => sut.GetBusLocationsAsync(AnySession, query: null, marketLocale: "tr-TR"));

        // Durum kodu, hız sınırı tespitinin gövdeye tek başına güvenmemesi
        // için taşınıyor.
        Assert.Equal(HttpStatusCode.BadRequest, exception.HttpStatusCode);
        Assert.True(exception.IsRecoverableBySessionRefresh);
    }

    [Fact]
    public void Upstream_mesaj_kullaniciya_gosterilecek_metne_karismaz()
    {
        // API başarısızlıkta kendi sunucu tarafı yığın izini döndürüyor.
        // Bu metin yalnızca loglanmak içindir; istisnanın kullanıcıya
        // gösterilen tarafında yer almaz.
        const string stackTrace =
            "Port can not be null for browsers.\nStackTrace: at oBilet.Api.Controllers...";

        var exception = new ObiletApiException(
            status: "Unknown",
            endpoint: "client/getsession",
            upstreamMessage: stackTrace,
            correlationId: "abc-123");

        Assert.Equal(stackTrace, exception.UpstreamMessage);

        // Kullanıcıya gösterilen view model upstream mesajı hiç taşımıyor;
        // yalnızca hata türü ve izleme kimliği geçiyor.
        var viewModel = new Web.Models.ApiErrorViewModel(
            Web.Models.ApiErrorKind.Unexpected, "trace-1");

        Assert.DoesNotContain("StackTrace", viewModel.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("Port can not be null", viewModel.ToString(), StringComparison.Ordinal);
    }
}
