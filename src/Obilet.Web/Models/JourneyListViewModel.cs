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
    DateOnly Today)
{
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
