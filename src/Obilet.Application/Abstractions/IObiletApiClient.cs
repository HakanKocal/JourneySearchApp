using Obilet.Application.Models;

namespace Obilet.Application.Abstractions;

/// <summary>
/// obilet business API'sine yapılan çağrıların soyutlaması. Uygulama katmanı
/// yalnızca bu arayüzü tanır; HTTP ayrıntıları Infrastructure katmanındadır.
/// </summary>
public interface IObiletApiClient
{
    /// <summary>
    /// Yeni bir Device Session oluşturur. Diğer tüm çağrılar bir Device Session
    /// gerektirdiği için ilk yapılması gereken çağrıdır.
    /// </summary>
    Task<DeviceSession> CreateSessionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Bus Location listesini döndürür.
    /// </summary>
    /// <param name="deviceSession">Çağrının adına yapılacağı Device Session.</param>
    /// <param name="query">
    /// Arama terimi. <c>null</c> verildiğinde API varsayılan listeyi döndürür.
    /// Bu listenin sistemdeki tüm lokasyonları içermediğine dikkat edin;
    /// gerekçe için bkz. docs/adr/0004.
    /// </param>
    /// <param name="marketLocale">
    /// API'ye gönderilecek Market Locale. Bu bir görüntü dili değil, pazar
    /// seçicidir ve doğrudan kullanıcı girdisinden gelmemelidir; bkz. docs/adr/0002.
    /// </param>
    Task<IReadOnlyList<BusLocation>> GetBusLocationsAsync(
        DeviceSession deviceSession,
        string? query,
        string marketLocale,
        CancellationToken cancellationToken = default);
}
