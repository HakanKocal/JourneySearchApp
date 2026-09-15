using Obilet.Application.Models;

namespace Obilet.Application.Locations;

/// <summary>
/// Bus Location'lara erişim sağlayan uygulama servisi.
/// </summary>
public interface ILocationService
{
    /// <summary>
    /// Arama formunun başlangıç listesini döndürür; API'nin verdiği sırayı korur.
    /// </summary>
    /// <remarks>
    /// Bu liste sistemdeki tüm Bus Location'ları içermez — API varsayılan
    /// çağrıda yalnızca sınırlı sayıda kayıt döndürür. Geri kalan lokasyonlara
    /// metin aramasıyla erişilir. Gerekçe için bkz. docs/adr/0004.
    ///
    /// Sıra önemlidir: varsayılan Origin ve Destination bu listenin ilk iki
    /// kaydından belirlenir.
    /// </remarks>
    Task<IReadOnlyList<BusLocation>> GetDefaultAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Arama terimiyle ilişkili Bus Location'ları döndürür.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Terim <see cref="MinimumQueryLength"/> karakterden kısaysa API'ye hiç
    /// gidilmez ve boş liste döner: tek harflik aramalar neredeyse her şeyi
    /// eşleştirdiği için kullanıcıya yardımcı olmuyor, API'ye ise gereksiz
    /// yük bindiriyor.
    /// </para>
    /// <para>
    /// Dönen sonuçlar terimle gerçekten ilişkili olup olmadıklarına göre
    /// süzülür; gerekçe için bkz. <see cref="LocationRelevance"/>.
    /// </para>
    /// </remarks>
    Task<IReadOnlyList<BusLocation>> SearchAsync(
        string? query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// API'ye gitmek için gereken en kısa arama terimi uzunluğu.
    /// </summary>
    const int MinimumQueryLength = 2;
}
