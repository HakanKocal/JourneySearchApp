using Obilet.Application.Journeys;
using Obilet.Application.Models;

namespace Obilet.Tests.Journeys;

/// <summary>
/// Sefer sıralamasını doğrular.
/// </summary>
/// <remarks>
/// Bu testlerin varlık sebebi canlı API'de doğrulanmış bir davranış: dönen
/// küme istenen günle sınırlı değil. İstanbul–Ankara sorgusunda 425 seferin
/// 53'ü ertesi güne, sabah 03:00'a kadar taşıyordu. Bu yüzden sıralamanın
/// tam tarih-saat değerine göre yapılması zorunlu; günün saatine göre
/// sıralamak ertesi günün gece seferini aynı günün akşam seferinin önüne atar.
/// </remarks>
public sealed class JourneyOrderingTests
{
    /// <summary>Yalnızca kimlik ve kalkış anı önemli olan bir sefer üretir.</summary>
    private static Journey JourneyAt(string departure, long id = 1) => new(
        Id: id,
        PartnerId: 1,
        PartnerName: "Test Turizm",
        BusType: "2+1",
        TotalSeats: 41,
        AvailableSeats: 10,
        OriginStation: "Esenler Otogarı",
        DestinationStation: "Ankara (Aşti) Otogarı",
        OriginLocation: "İstanbul Avrupa",
        DestinationLocation: "Ankara",
        Departure: DateTime.Parse(departure),
        Arrival: DateTime.Parse(departure).AddHours(6),
        Duration: TimeSpan.FromHours(6),
        OriginalPrice: 600,
        InternetPrice: 499,
        Currency: "TRY",
        Features: []);

    [Fact]
    public void Ayni_gun_icinde_kalkis_anina_gore_siralanir()
    {
        Journey[] unsorted =
        [
            JourneyAt("2026-09-16T09:00:00", 1),
            JourneyAt("2026-09-16T04:30:00", 2),
            JourneyAt("2026-09-16T23:50:00", 3),
            JourneyAt("2026-09-16T07:15:00", 4),
        ];

        var sorted = JourneyOrdering.ByDeparture(unsorted);

        Assert.Equal(
            ["04:30", "07:15", "09:00", "23:50"],
            sorted.Select(j => j.Departure.ToString("HH:mm")));
    }

    [Fact]
    public void Ertesi_gune_tasan_seferler_listenin_sonunda_kalir()
    {
        // Gerçek veri kümesinin şekli: aynı günün seferleri ve gece yarısını
        // aşıp ertesi sabaha uzanan birkaç sefer.
        Journey[] unsorted =
        [
            JourneyAt("2026-09-17T00:05:00", 1),
            JourneyAt("2026-09-16T23:50:00", 2),
            JourneyAt("2026-09-17T03:00:00", 3),
            JourneyAt("2026-09-16T04:30:00", 4),
        ];

        var sorted = JourneyOrdering.ByDeparture(unsorted);

        Assert.Equal(
            [
                "2026-09-16 04:30",
                "2026-09-16 23:50",
                "2026-09-17 00:05",
                "2026-09-17 03:00",
            ],
            sorted.Select(j => j.Departure.ToString("yyyy-MM-dd HH:mm")));
    }

    [Fact]
    public void Gunun_saatine_gore_siralamak_yanlis_sonuc_verirdi()
    {
        Journey[] journeys =
        [
            JourneyAt("2026-09-16T23:50:00", 1),
            JourneyAt("2026-09-17T00:05:00", 2),
        ];

        var correct = JourneyOrdering.ByDeparture(journeys);

        // Tuzağın kendisi: OrderBy(j => j.Departure.TimeOfDay) ertesi günün
        // 00:05 seferini aynı günün 23:50 seferinin önüne atar. Bu test,
        // birinin sıralamayı o şekilde "basitleştirmesini" engellemek için
        // iki sonucu açıkça karşılaştırıyor.
        var naive = journeys.OrderBy(j => j.Departure.TimeOfDay).ToList();

        Assert.Equal(1, correct[0].Id);
        Assert.Equal(2, naive[0].Id);
        Assert.NotEqual(correct[0].Id, naive[0].Id);
    }

    [Fact]
    public void Ayni_anda_kalkan_seferler_kararli_sirada_kalir()
    {
        Journey[] unsorted =
        [
            JourneyAt("2026-09-16T09:00:00", 30),
            JourneyAt("2026-09-16T09:00:00", 10),
            JourneyAt("2026-09-16T09:00:00", 20),
        ];

        var first = JourneyOrdering.ByDeparture(unsorted);
        var second = JourneyOrdering.ByDeparture(unsorted);

        // Aynı sorgu iki kez farklı sırada görünmemeli.
        Assert.Equal([10L, 20L, 30L], first.Select(j => j.Id));
        Assert.Equal(first.Select(j => j.Id), second.Select(j => j.Id));
    }

    [Fact]
    public void Bos_liste_bos_doner()
    {
        Assert.Empty(JourneyOrdering.ByDeparture([]));
    }
}
