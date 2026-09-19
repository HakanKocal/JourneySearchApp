using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Obilet.Application.Models;
using Obilet.Infrastructure.Obilet;
using Obilet.Tests.Fakes;

namespace Obilet.Tests.Obilet;

/// <summary>
/// Koltuk düzeninin API yanıtından nasıl okunduğunu doğrular.
/// </summary>
/// <remarks>
/// Alan bir süre modelde taşınıp hiç gösterilmiyordu; kartta gösterilmeye
/// başlandığında testsiz kaldığı ortaya çıktı. Değerin kendisi kullanıcıya
/// olduğu gibi yazıldığı için (<c>2+1</c>, <c>2+2</c>) doğru alandan
/// okunması ve bildirilmediğinde sessizce boş kalması önemli: eksik bir
/// değer kartta etiketi olan ama içeriği olmayan bir hücre bırakırdı.
/// </remarks>
public sealed class BusTypeMappingTests
{
    private static readonly DeviceSession AnySession = new("session", "device");

    private static string JourneyResponse(string busTypeField) => $$"""
        {
          "status": "Success",
          "data": [
            {
              "id": 1,
              "partner-id": 330,
              "partner-name": "Test Turizm",
              {{busTypeField}}
              "total-seats": 41,
              "available-seats": 10,
              "features": [],
              "journey": {
                "origin": "Esenler Otogarı",
                "destination": "Ankara (Aşti) Otogarı",
                "departure": "2026-09-16T09:00:00",
                "arrival": "2026-09-16T15:00:00",
                "duration": "06:00:00",
                "currency": "TRY",
                "original-price": 600,
                "internet-price": 499
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
            AnySession, 349, 356, new DateOnly(2026, 9, 16), "tr-TR");

        return Assert.Single(journeys);
    }

    [Theory]
    [InlineData("2+1")]
    [InlineData("2+2")]
    public async Task Koltuk_duzeni_oldugu_gibi_tasinir(string busType)
    {
        // Değer yorumlanmadan taşınıyor: "2+1" bir koltuk düzeni gösterimi
        // ve ayrıştırıp yeniden kurmak hiçbir şey kazandırmazdı.
        var journey = await SingleJourneyAsync(
            JourneyResponse($"\"bus-type\": \"{busType}\","));

        Assert.Equal(busType, journey.BusType);
    }

    [Fact]
    public async Task Koltuk_duzeni_bildirilmezse_bos_kalir()
    {
        // Alan hiç gelmeyebiliyor. Bu durumda kartta hücre çizilmiyor;
        // etiketi olup değeri olmayan bir hücre görünürse bu testin
        // kapsadığı varsayım bozulmuş olur.
        var journey = await SingleJourneyAsync(JourneyResponse(string.Empty));

        Assert.Null(journey.BusType);
    }

    [Fact]
    public async Task Koltuk_duzeni_null_gelirse_bos_kalir()
    {
        var journey = await SingleJourneyAsync(JourneyResponse("\"bus-type\": null,"));

        Assert.Null(journey.BusType);
    }
}
