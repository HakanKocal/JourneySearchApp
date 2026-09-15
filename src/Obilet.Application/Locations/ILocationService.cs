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
}
