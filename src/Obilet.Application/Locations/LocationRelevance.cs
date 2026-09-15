using Obilet.Application.Models;

namespace Obilet.Application.Locations;

/// <summary>
/// API'den dönen Bus Location'ların arama terimiyle gerçekten ilişkili olup
/// olmadığını belirler.
/// </summary>
/// <remarks>
/// <para>
/// Bu katmanın var olma sebebi canlı API'de doğrulanmış bir davranış:
/// <c>GetBusLocations</c> anlamsız bir arama terimine <b>boş liste
/// döndürmüyor</b>; sessizce en popüler lokasyonlara düşüyor. Örneğin
/// <c>xqjz</c> araması İstanbul, Ankara gibi sonuçlar veriyor.
/// </para>
/// <para>
/// Sonucu olduğu gibi göstermek kullanıcıya yalan söylemek olurdu: aradığı
/// şey bulunmadığı hâlde bulunmuş gibi bir liste sunulur. Bu yüzden dönen
/// kayıtlar terimle karşılaştırılır ve hiçbiri ilişkili değilse sonuç boş
/// sayılır.
/// </para>
/// <para>
/// Filtreleme API'nin bulanık eşleşmesini bozmaz: API'nin bulduğu ilişkili
/// sonuçlar aynen korunur, yalnızca ilişkisiz doldurma kayıtları atılır.
/// </para>
/// </remarks>
public static class LocationRelevance
{
    /// <summary>
    /// Bir Bus Location'ın arama terimiyle ilişkili olup olmadığını söyler.
    /// </summary>
    /// <remarks>
    /// Hem görünen ad hem de API'nin arama için tuttuğu anahtar kelimeler
    /// kontrol edilir. Anahtar kelimeler alanı yüzlerce karakter olabiliyor
    /// ve API'nin eşleşmelerinin çoğu oradan geliyor; yalnızca ada bakmak
    /// geçerli sonuçları da atardı.
    /// </remarks>
    public static bool IsRelevant(BusLocation location, string query) =>
        TurkishSearchText.Contains(location.Name, query)
        || TurkishSearchText.Contains(location.Keywords, query);

    /// <summary>
    /// Arama sonucundan ilişkisiz kayıtları ayıklar.
    /// </summary>
    /// <returns>
    /// Terimle ilişkili kayıtlar, API'nin verdiği sıra korunarak. Hiçbiri
    /// ilişkili değilse boş liste.
    /// </returns>
    public static IReadOnlyList<BusLocation> Filter(
        IEnumerable<BusLocation> locations,
        string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [.. locations];
        }

        return locations.Where(location => IsRelevant(location, query)).ToList();
    }
}
