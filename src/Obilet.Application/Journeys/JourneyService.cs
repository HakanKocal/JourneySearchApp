using Microsoft.Extensions.Logging;
using Obilet.Application.Abstractions;
using Obilet.Application.Exceptions;
using Obilet.Application.Localization;
using Obilet.Application.Models;

namespace Obilet.Application.Journeys;

/// <inheritdoc cref="IJourneyService"/>
public sealed class JourneyService : IJourneyService
{
    /// <summary>
    /// İki lokasyon arasında aktif hat bulunmadığını bildiren API durumu.
    /// </summary>
    private const string InvalidRouteStatus = "InvalidRoute";

    private readonly IObiletApiClient _apiClient;
    private readonly IObiletCallExecutor _executor;
    private readonly IMarketLocaleResolver _marketLocale;
    private readonly ILogger<JourneyService> _logger;

    public JourneyService(
        IObiletApiClient apiClient,
        IObiletCallExecutor executor,
        IMarketLocaleResolver marketLocale,
        ILogger<JourneyService> logger)
    {
        _apiClient = apiClient;
        _executor = executor;
        _marketLocale = marketLocale;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Journey>> SearchAsync(
        int originId,
        int destinationId,
        DateOnly departureDate,
        CancellationToken cancellationToken = default)
    {
        var marketLocale = _marketLocale.Resolve();

        // Sonuçlar bilinçli olarak önbelleklenmiyor: koltuk müsaitliği ve
        // dinamik fiyatlandırma nedeniyle sefer verisi hızla bayatlıyor.
        IReadOnlyList<Journey> journeys;
        try
        {
            journeys = await _executor.ExecuteAsync(
                (session, ct) => _apiClient.GetBusJourneysAsync(
                    session,
                    originId,
                    destinationId,
                    departureDate,
                    marketLocale,
                    ct),
                cancellationToken);
        }
        catch (ObiletApiException ex) when (IsEmptyResultStatus(ex.Status))
        {
            // İki lokasyon arasında hat olmaması bir arıza değil, geçerli bir
            // cevap. Kullanıcıya hata sayfası göstermek yerine boş sonuç
            // döndürüyoruz; arayüz bunu bilgilendirici bir durum olarak sunar.
            _logger.LogInformation(
                "{OriginId} → {DestinationId} için aktif hat bulunmadı ({Status}).",
                originId,
                destinationId,
                ex.Status);

            return [];
        }

        // API sıralı veri döndürmüyor ve küme ertesi güne taşıyor;
        // gerekçe için bkz. JourneyOrdering.
        return JourneyOrdering.ByDeparture(journeys);
    }

    private static bool IsEmptyResultStatus(string status) =>
        string.Equals(status, InvalidRouteStatus, StringComparison.OrdinalIgnoreCase);
}
