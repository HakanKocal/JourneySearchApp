using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Obilet.Application.Models;
using Obilet.Infrastructure.Obilet;
using Obilet.Tests.Fakes;

namespace Obilet.Tests.Obilet;

/// <summary>
/// Olanakların API yanıtından nasıl okunduğunu doğrular.
/// </summary>
/// <remarks>
/// En yüksek mevcut seam olan API istemcisi seviyesinden test edilir: doğru
/// alanın seçilmesi, gösterim sıralaması ve sayı sınırı tek yerden
/// gözlenebiliyor.
/// </remarks>
public sealed class FeatureMappingTests
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

    /// <param name="features">Sefer düzeyindeki olanak dizisi (doğru kaynak).</param>
    /// <param name="nestedFeatures">
    /// <c>journey.features</c> altındaki düz metin dizisi (yanlış kaynak).
    /// </param>
    private static string JourneyResponse(string features, string nestedFeatures = "[]") => $$"""
        {
          "status": "Success",
          "data": [
            {
              "id": 1,
              "partner-id": 330,
              "partner-name": "Test Turizm",
              "bus-type": "2+1",
              "total-seats": 41,
              "available-seats": 10,
              "features": {{features}},
              "journey": {
                "origin": "Esenler Otogarı",
                "destination": "Ankara (Aşti) Otogarı",
                "departure": "2026-09-16T09:00:00",
                "arrival": "2026-09-16T15:00:00",
                "duration": "06:00:00",
                "currency": "TRY",
                "original-price": 600,
                "internet-price": 499,
                "features": {{nestedFeatures}}
              }
            }
          ]
        }
        """;

    private async Task<Journey> SingleJourneyAsync(string body)
    {
        var sut = CreateSut(new StubHttpMessageHandler(HttpStatusCode.OK, body));

        var journeys = await sut.GetBusJourneysAsync(
            AnySession, 349, 356, new DateOnly(2026, 9, 16), "tr-TR");

        return Assert.Single(journeys);
    }

    [Fact]
    public async Task Olanaklar_ceviriye_uyan_alandan_okunur()
    {
        // İki kaynak bilinçli olarak farklı içerik taşıyor. Yanlış alanı
        // seçmek hata vermez, yalnızca İngilizce sayfada Türkçe metin
        // gösterir — sessizce başarısız olan bir değişiklik.
        var journey = await SingleJourneyAsync(JourneyResponse(
            features: """[ { "id": 10, "priority": 10, "name": "WIFI (Wireless Internet)" } ]""",
            nestedFeatures: """[ "Kablosuz Internet (WiFi)" ]"""));

        var feature = Assert.Single(journey.Features);
        Assert.Equal("WIFI (Wireless Internet)", feature.Name);
    }

    [Fact]
    public async Task Olanaklar_oncelik_sirasina_gore_dizilir()
    {
        // Canlı veride priority seyrek ve ardışık olmayan değerler alıyor.
        var journey = await SingleJourneyAsync(JourneyResponse("""
            [
              { "id": 25, "priority": 255, "name": "Rahat Koltuk" },
              { "id": 10, "priority": 10,  "name": "Kablosuz Internet (WiFi)" },
              { "id": 8,  "priority": 25,  "name": "USB ile Şarj İmkanı" },
              { "id": 7,  "priority": 13,  "name": "220 Voltluk Priz" }
            ]
            """));

        Assert.Equal(
            ["Kablosuz Internet (WiFi)", "220 Voltluk Priz", "USB ile Şarj İmkanı", "Rahat Koltuk"],
            journey.Features.Select(feature => feature.Name));
    }

    [Fact]
    public async Task Onceligi_olmayan_olanak_sona_dusar()
    {
        var journey = await SingleJourneyAsync(JourneyResponse("""
            [
              { "id": 99, "name": "Önceliksiz" },
              { "id": 10, "priority": 10, "name": "Kablosuz Internet (WiFi)" }
            ]
            """));

        Assert.Equal(
            ["Kablosuz Internet (WiFi)", "Önceliksiz"],
            journey.Features.Select(feature => feature.Name));
    }

    [Fact]
    public async Task Esit_oncelikte_sira_kararlidir()
    {
        // Aynı sorgu iki kez farklı sırada görünmemeli.
        var body = JourneyResponse("""
            [
              { "id": 30, "priority": 10, "name": "Uc" },
              { "id": 10, "priority": 10, "name": "Bir" },
              { "id": 20, "priority": 10, "name": "Iki" }
            ]
            """);

        var first = await SingleJourneyAsync(body);
        var second = await SingleJourneyAsync(body);

        Assert.Equal(["Bir", "Iki", "Uc"], first.Features.Select(f => f.Name));
        Assert.Equal(
            first.Features.Select(f => f.Name),
            second.Features.Select(f => f.Name));
    }

    [Fact]
    public async Task Gosterim_sayisi_sinirlanir_ve_toplam_korunur()
    {
        // Canlı veride bir seferin 8 olanağı olabiliyor.
        var items = Enumerable.Range(1, 8)
            .Select(i => $$"""{ "id": {{i}}, "priority": {{i}}, "name": "Olanak {{i}}" }""");

        var journey = await SingleJourneyAsync(
            JourneyResponse("[" + string.Join(",", items) + "]"));

        Assert.Equal(Feature.MaxDisplayed, journey.Features.Count);
        Assert.Equal(8, journey.TotalFeatureCount);
        Assert.Equal(8 - Feature.MaxDisplayed, journey.HiddenFeatureCount);

        // Kırpma sıranın sonundan yapılır; en önemliler kalır.
        Assert.Equal("Olanak 1", journey.Features[0].Name);
        Assert.Equal("Olanak 4", journey.Features[^1].Name);
    }

    [Fact]
    public async Task Sinirin_altinda_kirpma_gostergesi_cikmaz()
    {
        var journey = await SingleJourneyAsync(JourneyResponse("""
            [
              { "id": 10, "priority": 10, "name": "Kablosuz Internet (WiFi)" },
              { "id": 7,  "priority": 13, "name": "220 Voltluk Priz" }
            ]
            """));

        Assert.Equal(2, journey.Features.Count);
        Assert.Equal(0, journey.HiddenFeatureCount);
    }

    [Fact]
    public async Task Olanagi_olmayan_sefer_bos_liste_dondurur()
    {
        // Canlı veride 427 seferin 103'ünde hiç olanak yok; null yerine boş
        // liste dönmesi görünümün kontrol yapmasını gerektirmiyor.
        var journey = await SingleJourneyAsync(JourneyResponse("[]"));

        Assert.Empty(journey.Features);
        Assert.Equal(0, journey.TotalFeatureCount);
    }

    [Fact]
    public async Task Olanak_alani_hic_yoksa_bos_liste_dondurur()
    {
        const string body = """
            {
              "status": "Success",
              "data": [
                {
                  "id": 1, "partner-id": 330, "partner-name": "Test Turizm",
                  "journey": {
                    "origin": "A", "destination": "B",
                    "departure": "2026-09-16T09:00:00",
                    "arrival": "2026-09-16T15:00:00",
                    "currency": "TRY", "original-price": 600, "internet-price": 499
                  }
                }
              ]
            }
            """;

        var journey = await SingleJourneyAsync(body);

        Assert.Empty(journey.Features);
    }

    [Fact]
    public async Task Adi_olmayan_olanak_ayiklanir()
    {
        // Adsız bir olanak boş bir ikondan başka bir şey üretmezdi.
        var journey = await SingleJourneyAsync(JourneyResponse("""
            [
              { "id": 10, "priority": 10, "name": "Kablosuz Internet (WiFi)" },
              { "id": 11, "priority": 11, "name": "" },
              { "id": 12, "priority": 12, "name": "   " },
              { "id": 13, "priority": 13, "name": null }
            ]
            """));

        var feature = Assert.Single(journey.Features);
        Assert.Equal("Kablosuz Internet (WiFi)", feature.Name);
        Assert.Equal(1, journey.TotalFeatureCount);
    }

    [Fact]
    public async Task Tanitimli_olanak_renklerini_korur()
    {
        // Canlı veride gözlenen gerçek değerler.
        var journey = await SingleJourneyAsync(JourneyResponse("""
            [
              {
                "id": 202, "priority": 5, "name": "75₺ İndirim Kodu",
                "is-promoted": true, "back-color": "#dffadc", "fore-color": "#7C8A9F"
              }
            ]
            """));

        var feature = Assert.Single(journey.Features);
        Assert.True(feature.IsPromoted);
        Assert.Equal("#dffadc", feature.BackgroundColor);
        Assert.Equal("#7C8A9F", feature.ForegroundColor);
    }

    [Fact]
    public async Task Tanitimli_olmayan_olanak_renk_tasimaz()
    {
        // API tanıtımlı olmayan kayıtlar için de renk gönderirse yok sayılır:
        // renkli etiket yalnızca tanıtımlı olanların gösterim biçimi.
        var journey = await SingleJourneyAsync(JourneyResponse("""
            [
              {
                "id": 10, "priority": 10, "name": "Kablosuz Internet (WiFi)",
                "is-promoted": false, "back-color": "#dffadc", "fore-color": "#7C8A9F"
              }
            ]
            """));

        var feature = Assert.Single(journey.Features);
        Assert.False(feature.IsPromoted);
        Assert.Null(feature.BackgroundColor);
        Assert.Null(feature.ForegroundColor);
    }

    [Fact]
    public async Task Olanak_adindaki_bosluklar_kirpilir()
    {
        var journey = await SingleJourneyAsync(JourneyResponse("""
            [ { "id": 10, "priority": 10, "name": "  Kablosuz Internet (WiFi)  " } ]
            """));

        Assert.Equal("Kablosuz Internet (WiFi)", Assert.Single(journey.Features).Name);
    }
}
