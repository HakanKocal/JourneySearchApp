using Obilet.Application.Models;

namespace Obilet.Application.Abstractions;

/// <summary>
/// Geçerli ziyaretçinin Device Session'ına erişim sağlar.
/// </summary>
public interface IDeviceSessionAccessor
{
    /// <summary>
    /// Ziyaretçinin Device Session'ını döndürür. Henüz yoksa oluşturup
    /// Visitor Session içine yazar; varsa yeniden kullanır.
    /// </summary>
    Task<DeviceSession> GetOrCreateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Mevcut Device Session'ı atıp yenisini oluşturur. API bir isteği
    /// oturumun geçersizliği nedeniyle reddettiğinde kullanılır.
    /// </summary>
    Task<DeviceSession> RefreshAsync(CancellationToken cancellationToken = default);
}
