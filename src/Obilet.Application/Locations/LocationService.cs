using Obilet.Application.Abstractions;
using Obilet.Application.Models;

namespace Obilet.Application.Locations;

/// <inheritdoc cref="ILocationService"/>
public sealed class LocationService : ILocationService
{
    private readonly IObiletApiClient _apiClient;
    private readonly IObiletCallExecutor _executor;

    public LocationService(IObiletApiClient apiClient, IObiletCallExecutor executor)
    {
        _apiClient = apiClient;
        _executor = executor;
    }

    public Task<IReadOnlyList<BusLocation>> GetDefaultAsync(
        CancellationToken cancellationToken = default)
    {
        // Arama terimi verilmediğinde API varsayılan, sıralanmış listeyi döndürür.
        return _executor.ExecuteAsync(
            (session, ct) => _apiClient.GetBusLocationsAsync(
                session,
                query: null,
                marketLocale: ObiletDefaults.MarketLocale,
                cancellationToken: ct),
            cancellationToken);
    }
}
