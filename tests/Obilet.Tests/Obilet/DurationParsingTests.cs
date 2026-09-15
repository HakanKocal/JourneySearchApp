using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Obilet.Application.Models;
using Obilet.Infrastructure.Obilet;
using Obilet.Tests.Fakes;

namespace Obilet.Tests.Obilet;

/// <summary>
/// Süre alanının ayrıştırılmasını doğrular.
/// </summary>
/// <remarks>
/// Bir kod incelemesinde bulunan uçurumu koruyor: varsayılan
/// <see cref="TimeSpan"/> dönüştürücüsü saat bileşeninin 00–23 aralığında
/// olmasını bekliyor. Ölçülen davranış: <c>"1.01:30:00"</c> ayrıştırılıyor,
/// <c>"25:30:00"</c> <see cref="JsonException"/> fırlatıyor. API doküman
/// süreyi <c>HH:MM:SS</c> olarak belgelediği için 24 saati aşan bir sefer
/// tüm sefer listesini hata sayfasına çevirebilirdi.
/// </remarks>
public sealed class DurationParsingTests
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

    private static string JourneyResponseWithDuration(string duration) => $$"""
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
              "journey": {
                "origin": "Esenler Otogarı",
                "destination": "Ankara (Aşti) Otogarı",
                "departure": "2026-09-16T09:00:00",
                "arrival": "2026-09-17T10:30:00",
                "duration": "{{duration}}",
                "currency": "TRY",
                "original-price": 600,
                "internet-price": 499
              }
            }
          ]
        }
        """;

    [Theory]
    [InlineData("07:00:00", 7, 0)]
    [InlineData("1.01:30:00", 25, 30)]
    [InlineData("25:30:00", 25, 30)]
    [InlineData("48:00:00", 48, 0)]
    public async Task Uzun_sureler_de_ayristirilir(string raw, int hours, int minutes)
    {
        var sut = CreateSut(new StubHttpMessageHandler(
            HttpStatusCode.OK, JourneyResponseWithDuration(raw)));

        var journeys = await sut.GetBusJourneysAsync(
            AnySession, 349, 356, new DateOnly(2026, 9, 16), "tr-TR");

        Assert.Single(journeys);
        Assert.Equal(new TimeSpan(hours, minutes, 0), journeys[0].Duration);
    }

    [Fact]
    public async Task Ayristirilamayan_sure_tum_yaniti_dusurmez()
    {
        // Asıl korunan davranış: süre gösterilemeyen bir ayrıntı, sefer
        // listesinin tamamını hata sayfasına çevirmeye değmez.
        var sut = CreateSut(new StubHttpMessageHandler(
            HttpStatusCode.OK, JourneyResponseWithDuration("bilinmiyor")));

        var journeys = await sut.GetBusJourneysAsync(
            AnySession, 349, 356, new DateOnly(2026, 9, 16), "tr-TR");

        Assert.Single(journeys);
        Assert.Null(journeys[0].Duration);
        Assert.Equal("Test Turizm", journeys[0].PartnerName);
    }

    [Theory]
    [InlineData("07:00:00", 7, 0, 0)]
    [InlineData("25:30:00", 25, 30, 0)]
    [InlineData("1.01:30:00", 25, 30, 0)]
    [InlineData("06:05", 6, 5, 0)]
    [InlineData("06:05:30", 6, 5, 30)]
    public void Donusturucu_dogrudan_da_ayristirir(
        string raw, int hours, int minutes, int seconds)
    {
        Assert.Equal(
            new TimeSpan(hours, minutes, seconds),
            TolerantTimeSpanConverter.Parse(raw));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("bilinmiyor")]
    [InlineData("25:99:00")]
    [InlineData("1:2:3:4")]
    public void Gecersiz_degerler_null_doner(string? raw)
    {
        Assert.Null(TolerantTimeSpanConverter.Parse(raw));
    }
}
