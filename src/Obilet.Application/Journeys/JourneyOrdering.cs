using Obilet.Application.Models;

namespace Obilet.Application.Journeys;

/// <summary>
/// Journey listesinin görüntüleme sırası.
/// </summary>
public static class JourneyOrdering
{
    /// <summary>
    /// Seferleri kalkış anına göre artan sırada döndürür.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Sıralama <b>tam tarih-saat</b> değerine göre yapılır, günün saatine
    /// göre değil. Bu bir ayrıntı değil, doğruluk meselesi: API'nin döndürdüğü
    /// küme istenen günle sınırlı değil ve gece yarısını aşan seferler ertesi
    /// güne taşıyor. Günün saatine göre sıralamak, ertesi günün 00:05
    /// otobüsünü aynı günün 23:50 otobüsünün önüne atardı.
    /// </para>
    /// <para>
    /// Yani <c>OrderBy(j =&gt; j.Departure.TimeOfDay)</c> bu veri kümesinde
    /// yanlıştır. Bir test bu tuzağı koruyor.
    /// </para>
    /// <para>
    /// API sıralı veri döndürmüyor, dolayısıyla bu adım atlanamaz.
    /// </para>
    /// </remarks>
    public static IReadOnlyList<Journey> ByDeparture(IEnumerable<Journey> journeys) =>
        journeys
            .OrderBy(journey => journey.Departure)
            // Aynı anda kalkan seferler arasında kararlı ve öngörülebilir bir
            // sıra kalsın: aksi hâlde aynı sorgu iki kez farklı sırada görünür.
            .ThenBy(journey => journey.Id)
            .ToList();
}
