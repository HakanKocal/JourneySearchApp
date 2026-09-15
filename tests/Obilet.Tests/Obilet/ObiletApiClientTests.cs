using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Obilet.Application.Exceptions;
using Obilet.Application.Models;
using Obilet.Infrastructure.Obilet;
using Obilet.Tests.Fakes;

namespace Obilet.Tests.Obilet;

/// <summary>
/// API istemcisinin yanıt yorumlamasını doğrular.
/// </summary>
/// <remarks>
/// Bu testlerin varlık sebebi canlı API'de doğrulanmış bir davranış:
/// başarısızlıklar çoğunlukla HTTP 200 ile dönüyor ve başarı yalnızca yanıt
/// gövdesindeki durum alanından anlaşılıyor. HTTP durum koduna güvenen bir
/// uygulama hatayı sessizce başarı sayardı.
/// </remarks>
public sealed class ObiletApiClientTests
{
    private static readonly DeviceSession AnySession = new("session", "device");

    private static ObiletApiClient CreateSut(
        StubHttpMessageHandler handler,
        string baseUrl = "https://example.invalid/api/")
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri(baseUrl) };

        var options = Options.Create(new ObiletApiOptions
        {
            BaseUrl = baseUrl,
            ApiClientToken = "test-token",
        });

        return new ObiletApiClient(httpClient, options, NullLogger<ObiletApiClient>.Instance);
    }

    [Fact]
    public async Task HTTP_200_icinde_gelen_hata_istisnaya_cevrilir()
    {
        // Canlı API, geçersiz bir oturum isteğine tam olarak böyle yanıt veriyor:
        // 200 OK, ancak gövdede başarısız bir durum ve sunucu tarafı yığın izi.
        const string body = """
            {
              "status": "Unknown",
              "data": null,
              "message": "Port can not be null for browsers.\nStackTrace: at oBilet.Api...",
              "correlation-id": "abc-123"
            }
            """;

        var sut = CreateSut(new StubHttpMessageHandler(HttpStatusCode.OK, body));

        var exception = await Assert.ThrowsAsync<ObiletApiException>(
            () => sut.CreateSessionAsync());

        Assert.Equal("Unknown", exception.Status);
        Assert.Equal("abc-123", exception.CorrelationId);
        Assert.False(exception.IsRecoverableBySessionRefresh);
    }

    [Fact]
    public async Task Oturum_hatasi_kurtarilabilir_olarak_isaretlenir()
    {
        // Geçersiz Device Session, diğer hataların aksine HTTP 400 ile geliyor;
        // yeniden deneme kararı HTTP koduna değil, durum değerine bakar.
        const string body = """
            { "status": "DeviceSessionError", "data": null, "message": "invalid session" }
            """;

        var sut = CreateSut(new StubHttpMessageHandler(HttpStatusCode.BadRequest, body));

        var exception = await Assert.ThrowsAsync<ObiletApiException>(
            () => sut.GetBusLocationsAsync(AnySession, query: null, marketLocale: "tr-TR"));

        Assert.True(exception.IsRecoverableBySessionRefresh);
    }

    [Fact]
    public async Task Basarili_yanitta_oturum_cozulur()
    {
        const string body = """
            {
              "status": "Success",
              "data": { "session-id": "abc=", "device-id": "def=" },
              "message": null
            }
            """;

        var sut = CreateSut(new StubHttpMessageHandler(HttpStatusCode.OK, body));

        var session = await sut.CreateSessionAsync();

        Assert.Equal("abc=", session.SessionId);
        Assert.Equal("def=", session.DeviceId);
    }

    [Fact]
    public async Task Basarili_ama_bos_oturum_kabul_edilmez()
    {
        // Durum başarılı olduğu hâlde kimlik çifti boşsa devam etmek anlamsız:
        // sonraki her çağrı zaten reddedilirdi.
        const string body = """
            { "status": "Success", "data": { "session-id": null, "device-id": null } }
            """;

        var sut = CreateSut(new StubHttpMessageHandler(HttpStatusCode.OK, body));

        await Assert.ThrowsAsync<ObiletApiException>(() => sut.CreateSessionAsync());
    }

    [Fact]
    public async Task Oturum_istegi_dokumandaki_yerine_calisan_govdeyi_gonderir()
    {
        const string body = """
            { "status": "Success", "data": { "session-id": "a", "device-id": "b" } }
            """;

        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, body);
        var sut = CreateSut(handler);

        await sut.CreateSessionAsync();

        // Resmî doküman "type": 7 ve bir "application" nesnesi belgeliyor, ancak
        // API bunu reddediyor. Bu test, çalışan gövdeye yanlışlıkla geri
        // dönülmesini engeller.
        Assert.NotNull(handler.LastRequestBody);
        Assert.Contains("\"type\":1", handler.LastRequestBody);
        Assert.Contains("\"port\"", handler.LastRequestBody);
        Assert.Contains("\"browser\"", handler.LastRequestBody);
        Assert.DoesNotContain("\"application\"", handler.LastRequestBody);
    }

    [Fact]
    public async Task Bus_location_istegi_kebab_case_sozlesmesini_kullanir()
    {
        const string body = """
            {
              "status": "Success",
              "data": [ { "id": 349, "name": "İstanbul Avrupa", "rank": 1, "keywords": "k" } ]
            }
            """;

        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, body);
        var sut = CreateSut(handler);

        var locations = await sut.GetBusLocationsAsync(
            AnySession, query: null, marketLocale: "tr-TR");

        Assert.Single(locations);
        Assert.Equal(349, locations[0].Id);
        Assert.Equal("İstanbul Avrupa", locations[0].Name);
        Assert.Equal(1, locations[0].Rank);

        // API sözleşmesi baştan sona kebab-case; alan adları bozulursa
        // istek sessizce boş sonuç döndürür.
        Assert.Contains("\"device-session\"", handler.LastRequestBody);
        Assert.Contains("\"session-id\"", handler.LastRequestBody);
        Assert.Contains("\"data\":null", handler.LastRequestBody);
    }
}
