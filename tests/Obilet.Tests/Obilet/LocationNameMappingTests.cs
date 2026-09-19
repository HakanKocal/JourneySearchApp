using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Obilet.Application.Models;
using Obilet.Infrastructure.Obilet;
using Obilet.Tests.Fakes;

namespace Obilet.Tests.Obilet;

/// <summary>
/// Sefer kaydındaki lokasyon adlarının doğru alanlardan okunduğunu doğrular.
/// </summary>
/// <remarks>
/// <para>
/// Bu testler bir hatadan doğdu. Sefer sayfası lokasyon adlarını varsayılan
/// 20 kayıtlık listeden çözüyordu; o listede olmayan bir lokasyon seçildiğinde
/// ada değil kimliğe düşüyor ve kullanıcı "Rize" yerine "400" görüyordu.
/// API adı her sefer kaydında zaten gönderiyor.
/// </para>
/// <para>
/// Kolay karıştırılan iki alan çifti var ve testler ikisini bilinçli olarak
/// farklı değerlerle besliyor: <c>origin-location</c> lokasyonun (şehrin)
/// adını, <c>journey.origin</c> ise terminalin adını taşıyor. Yanlış alanı
/// seçmek hata vermez, yalnızca ekranda yanlış metin gösterir.
/// </para>
/// </remarks>
public sealed class LocationNameMappingTests
{
    private static readonly DeviceSession AnySession = new("session", "device");

    private static string JourneyResponse(string locationFields) => $$"""
        {
          "status": "Success",
          "data": [
            {
              "id": 1,
              "partner-id": 330,
              "partner-name": "Test Turizm",
              "bus-type": "2+1",
              {{locationFields}}
              "features": [],
              "journey": {
                "origin": "Esenler Otogarı",
                "destination": "Rize Otogarı",
                "departure": "2026-09-20T21:00:00",
                "arrival": "2026-09-21T15:00:00",
                "duration": "18:00:00",
                "currency": "TRY",
                "original-price": 900,
                "internet-price": 800
              }
            }
          ]
        }
        """;

    private static async Task<Journey> SingleJourneyAsync(string body)
    {
        var httpClient = new HttpClient(new StubHttpMessageHandler(HttpStatusCode.OK, body))
        {
            BaseAddress = new Uri("https://example.invalid/api/"),
        };

        var sut = new ObiletApiClient(
            httpClient,
            Options.Create(new ObiletApiOptions
            {
                BaseUrl = "https://example.invalid/api/",
                ApiClientToken = "test-token",
            }),
            new StubMarketClock(),
            NullLogger<ObiletApiClient>.Instance);

        var journeys = await sut.GetBusJourneysAsync(
            AnySession, 349, 400, new DateOnly(2026, 9, 20), "tr-TR");

        return Assert.Single(journeys);
    }

    [Fact]
    public async Task Lokasyon_adlari_sefer_kaydindan_okunur()
    {
        var journey = await SingleJourneyAsync(JourneyResponse("""
            "origin-location": "İstanbul Avrupa",
            "destination-location": "Rize",
            """));

        Assert.Equal("İstanbul Avrupa", journey.OriginLocation);
        Assert.Equal("Rize", journey.DestinationLocation);
    }

    [Fact]
    public async Task Lokasyon_adi_terminal_adiyla_karistirilmaz()
    {
        // İki alan çifti farklı bilgi taşıyor ve kartta ikisi de gösteriliyor.
        // Bu test yanlış alana bağlanmayı yakalar.
        var journey = await SingleJourneyAsync(JourneyResponse("""
            "origin-location": "İstanbul Avrupa",
            "destination-location": "Rize",
            """));

        Assert.Equal("Esenler Otogarı", journey.OriginStation);
        Assert.Equal("Rize Otogarı", journey.DestinationStation);

        Assert.NotEqual(journey.OriginStation, journey.OriginLocation);
        Assert.NotEqual(journey.DestinationStation, journey.DestinationLocation);
    }

    [Fact]
    public async Task Lokasyon_adlari_bildirilmezse_bos_kalir()
    {
        // Alanlar hiç gelmezse sayfa bozulmamalı: çağıran taraf varsayılan
        // listeye düşüyor.
        var journey = await SingleJourneyAsync(JourneyResponse(string.Empty));

        Assert.Null(journey.OriginLocation);
        Assert.Null(journey.DestinationLocation);
    }
}
