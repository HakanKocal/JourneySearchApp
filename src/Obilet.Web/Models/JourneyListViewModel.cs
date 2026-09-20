using Obilet.Application.Models;

namespace Obilet.Web.Models;

/// <summary>
/// Sefer listesi sayfasının view model'i.
/// </summary>
/// <param name="Journeys">Kalkış anına göre artan sırada seferler.</param>
/// <param name="OriginId">Sorgulanan kalkış lokasyonunun kimliği.</param>
/// <param name="DestinationId">Sorgulanan varış lokasyonunun kimliği.</param>
/// <param name="DepartureDate">Sorgulanan kalkış günü.</param>
/// <param name="OriginName">Kalkış lokasyonunun adı; bulunamazsa <c>null</c>.</param>
/// <param name="DestinationName">Varış lokasyonunun adı; bulunamazsa <c>null</c>.</param>
/// <param name="Today">
/// Pazarın saat diliminde bugünün tarihi.
/// </param>
/// <param name="Locations">
/// Sayfadaki arama formunun açılır listelerini besleyen Bus Location'lar.
/// </param>
/// <remarks>
/// <para>
/// <paramref name="Today"/> yalnızca bir görüntü değeri değil: sayfadaki
/// "bugün" ve "yarın" çipleri bu tarihten üretiliyor. Sunucunun saat
/// dilimi değil pazarın saat dilimi kullanılır, aksi hâlde konteyner UTC
/// çalışırken gece saatlerinde çipler bir gün geride kalırdı;
/// bkz. <c>IMarketClock</c>.
/// </para>
/// </remarks>
public sealed record JourneyListViewModel(
    IReadOnlyList<Journey> Journeys,
    int OriginId,
    int DestinationId,
    DateOnly DepartureDate,
    string? OriginName,
    string? DestinationName,
    DateOnly Today,
    IReadOnlyList<BusLocation> Locations)
{
    /// <summary>
    /// Açılır listede sorgulanan lokasyonun bulunup bulunmadığını söyler.
    /// </summary>
    /// <remarks>
    /// Varsayılan liste sistemdeki tüm lokasyonları içermiyor (bkz.
    /// docs/adr/0004). Kullanıcı arama yoluyla listede olmayan bir lokasyon
    /// seçmiş olabilir; o durumda seçenek listeye elle eklenmeli, aksi hâlde
    /// açılır liste sorgulanan lokasyonu hiç göstermez ve tarayıcı seçimi
    /// ilk seçeneğe kaydırır — kullanıcı formu açtığında aradığı güzergâh
    /// yerine başka bir şey görürdü.
    /// </remarks>
    public bool IsListed(int id) => Locations.Any(location => location.Id == id);

    /// <summary>Hiç sefer bulunamadı mı?</summary>
    /// <remarks>
    /// Boş sonuç bir hata değildir: seçilen gün için sefer olmaması veya iki
    /// lokasyon arasında aktif hat bulunmaması normal sonuçlardır.
    /// </remarks>
    public bool IsEmpty => Journeys.Count == 0;

    /// <summary>
    /// Sorgulanan günden sonraki güne taşan seferlerin ilk sırası.
    /// </summary>
    /// <remarks>
    /// API'nin döndürdüğü küme istenen günle sınırlı değil; gece yarısını
    /// aşan seferler ertesi güne taşıyor. Liste kronolojik sırada olduğu
    /// için bu seferler sonda toplanır ve arayüz araya bir ayırıcı koyabilir.
    /// </remarks>
    public int? FirstNextDayIndex
    {
        get
        {
            for (var index = 0; index < Journeys.Count; index++)
            {
                if (DateOnly.FromDateTime(Journeys[index].Departure) > DepartureDate)
                {
                    return index;
                }
            }

            return null;
        }
    }
}
