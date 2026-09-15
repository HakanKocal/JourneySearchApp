using Obilet.Application.Models;

namespace Obilet.Application.Journeys;

/// <summary>
/// Journey aramasını yürüten uygulama servisi.
/// </summary>
public interface IJourneyService
{
    /// <summary>
    /// Verilen Search Query için uygun Journey listesini, kalkış anına göre
    /// artan sırada döndürür.
    /// </summary>
    /// <remarks>
    /// Sefer bulunamaması veya iki lokasyon arasında aktif hat olmaması
    /// birer hata değil, normal sonuçtur; bu durumlarda boş liste döner.
    /// </remarks>
    Task<IReadOnlyList<Journey>> SearchAsync(
        int originId,
        int destinationId,
        DateOnly departureDate,
        CancellationToken cancellationToken = default);
}
