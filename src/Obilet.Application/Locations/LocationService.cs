using Obilet.Application.Abstractions;
using Obilet.Application.Localization;
using Obilet.Application.Models;

namespace Obilet.Application.Locations;

/// <inheritdoc cref="ILocationService"/>
public sealed class LocationService : ILocationService
{
    private readonly IObiletApiClient _apiClient;
    private readonly IObiletCallExecutor _executor;
    private readonly IMarketLocaleResolver _marketLocale;

    public LocationService(
        IObiletApiClient apiClient,
        IObiletCallExecutor executor,
        IMarketLocaleResolver marketLocale)
    {
        _apiClient = apiClient;
        _executor = executor;
        _marketLocale = marketLocale;
    }

    public Task<IReadOnlyList<BusLocation>> GetDefaultAsync(
        CancellationToken cancellationToken = default)
    {
        // Market Locale doğrudan kullanıcı girdisinden değil, beyaz listeden
        // geçen çözümleyiciden gelir; bkz. docs/adr/0002.
        var marketLocale = _marketLocale.Resolve();

        // Arama terimi verilmediğinde API varsayılan, sıralanmış listeyi döndürür.
        return _executor.ExecuteAsync(
            (session, ct) => _apiClient.GetBusLocationsAsync(
                session,
                query: null,
                marketLocale: marketLocale,
                cancellationToken: ct),
            cancellationToken);
    }
}
