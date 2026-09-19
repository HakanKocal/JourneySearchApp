using Microsoft.Extensions.Logging.Abstractions;
using Obilet.Application.Exceptions;
using Obilet.Application.Journeys;
using Obilet.Application.Localization;
using Obilet.Application.Models;
using Obilet.Application.Sessions;
using Obilet.Tests.Fakes;

namespace Obilet.Tests.Journeys;

/// <summary>
/// Sefer arama servisinin davranışını doğrular.
/// </summary>
public sealed class JourneyServiceTests
{
    private readonly FakeObiletApiClient _apiClient = new();
    private readonly FakeVisitorSessionStore _visitorSession = new();

    private JourneyService CreateSut()
    {
        var accessor = new DeviceSessionAccessor(
            _apiClient, _visitorSession, NullLogger<DeviceSessionAccessor>.Instance);

        var executor = new ObiletCallExecutor(
            accessor, NullLogger<ObiletCallExecutor>.Instance);

        return new JourneyService(
            _apiClient,
            executor,
            new MarketLocaleResolver(),
            NullLogger<JourneyService>.Instance);
    }

    private static Journey JourneyAt(string departure, long id = 1) => new(
        Id: id,
        PartnerId: 1,
        PartnerName: "Test Turizm",
        BusType: "2+1",
        TotalSeats: 41,
        AvailableSeats: 10,
        OriginStation: "Esenler Otogarı",
        DestinationStation: "Ankara (Aşti) Otogarı",
        Departure: DateTime.Parse(departure),
        Arrival: DateTime.Parse(departure).AddHours(6),
        Duration: TimeSpan.FromHours(6),
        OriginalPrice: 600,
        InternetPrice: 499,
        Currency: "TRY",
        Features: []);

    [Fact]
    public async Task Sonuclar_kalkis_anina_gore_sirali_doner()
    {
        _apiClient.Journeys =
        [
            JourneyAt("2026-09-17T00:05:00", 1),
            JourneyAt("2026-09-16T04:30:00", 2),
            JourneyAt("2026-09-16T23:50:00", 3),
        ];

        var sut = CreateSut();

        var result = await sut.SearchAsync(349, 356, new DateOnly(2026, 9, 16));

        // API sıralı veri döndürmüyor; sıralama servisin sorumluluğunda.
        Assert.Equal([2L, 3L, 1L], result.Select(j => j.Id));
    }

    [Fact]
    public async Task Aktif_hat_yoksa_hata_degil_bos_liste_doner()
    {
        _apiClient.JourneySearchException = new ObiletApiException(
            status: "InvalidRoute",
            endpoint: "journey/getbusjourneys",
            upstreamMessage: "Invalid route",
            correlationId: null);

        var sut = CreateSut();

        var result = await sut.SearchAsync(349, 349, new DateOnly(2026, 9, 16));

        // İki lokasyon arasında hat olmaması geçerli bir cevap. Kullanıcıya
        // hata sayfası göstermek yerine boş sonuç sunulur.
        Assert.Empty(result);
    }

    [Fact]
    public async Task Diger_API_hatalari_yukari_kabarir()
    {
        _apiClient.JourneySearchException = new ObiletApiException(
            status: "Timeout",
            endpoint: "journey/getbusjourneys",
            upstreamMessage: null,
            correlationId: null);

        var sut = CreateSut();

        // Gerçek bir arıza boş sonuç gibi gösterilmemeli: kullanıcıya "sefer
        // yok" demek, aslında sistem hatası varken yanlış bilgi vermek olurdu.
        await Assert.ThrowsAsync<ObiletApiException>(
            () => sut.SearchAsync(349, 356, new DateOnly(2026, 9, 16)));
    }

    [Fact]
    public async Task Market_locale_beyaz_listeden_gecirilerek_gonderilir()
    {
        var sut = CreateSut();

        await sut.SearchAsync(349, 356, new DateOnly(2026, 9, 16));

        Assert.Contains(_apiClient.LastMarketLocale, MarketLocale.Supported);
        Assert.NotEqual(MarketLocale.DocumentedButBroken, _apiClient.LastMarketLocale);
    }

    [Fact]
    public async Task Bos_sonuc_sorunsuz_islenir()
    {
        _apiClient.Journeys = [];

        var sut = CreateSut();

        var result = await sut.SearchAsync(349, 356, new DateOnly(2026, 9, 16));

        Assert.Empty(result);
    }
}
