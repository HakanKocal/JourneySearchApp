using Obilet.Application.Models;

namespace Obilet.Application.Caching;

/// <summary>
/// Varsayılan Bus Location listesinin önbelleği.
/// </summary>
/// <remarks>
/// <para>
/// Yalnızca varsayılan liste önbelleklenir; arama sonuçları önbelleklenmez.
/// Arama terimlerinin kuyruğu çok uzun, isabet oranı düşük ve her terim ayrı
/// bir giriş açacağı için bellek gereksiz şişerdi.
/// </para>
/// <para>
/// Önbellek Market Locale başına anahtarlanır. Bu zorunludur: aynı çağrı
/// farklı bir Market Locale ile farklı adlar döndürür ve ortak bir anahtar,
/// Türkçe isimleri İngilizce arayüze sızdırırdı.
/// </para>
/// <para>
/// Önbellek girişleri Device Session'dan bağımsızdır; lokasyon verisi
/// kullanıcıya özel değildir ve ziyaretçiler arasında paylaşılabilir.
/// </para>
/// </remarks>
public interface ILocationCache
{
    /// <summary>
    /// Verilen Market Locale için önbellekteki listeyi döndürür; yoksa
    /// <paramref name="factory"/> ile üretip önbelleğe yazar.
    /// </summary>
    Task<IReadOnlyList<BusLocation>> GetOrCreateDefaultAsync(
        string marketLocale,
        Func<CancellationToken, Task<IReadOnlyList<BusLocation>>> factory,
        CancellationToken cancellationToken = default);
}
