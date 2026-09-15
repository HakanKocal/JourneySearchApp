using Obilet.Application.Abstractions;
using Obilet.Application.Caching;
using Obilet.Application.Localization;
using Obilet.Application.Models;

namespace Obilet.Application.Locations;

/// <inheritdoc cref="ILocationService"/>
public sealed class LocationService : ILocationService
{
    private readonly IObiletApiClient _apiClient;
    private readonly IObiletCallExecutor _executor;
    private readonly IMarketLocaleResolver _marketLocale;
    private readonly ILocationCache _cache;

    public LocationService(
        IObiletApiClient apiClient,
        IObiletCallExecutor executor,
        IMarketLocaleResolver marketLocale,
        ILocationCache cache)
    {
        _apiClient = apiClient;
        _executor = executor;
        _marketLocale = marketLocale;
        _cache = cache;
    }

    public async Task<IReadOnlyList<BusLocation>> SearchAsync(
        string? query,
        CancellationToken cancellationToken = default)
    {
        var trimmed = query?.Trim() ?? string.Empty;

        // Çok kısa terimler için API'ye hiç gitmiyoruz: tek harf neredeyse
        // her şeyi eşleştiriyor, dolayısıyla kullanıcıya yardımcı olmuyor.
        if (trimmed.Length < ILocationService.MinimumQueryLength)
        {
            return [];
        }

        var marketLocale = _marketLocale.Resolve();

        // Arama sonuçları bilinçli olarak önbelleklenmiyor: terim kuyruğu
        // çok uzun, isabet oranı düşük ve her terim ayrı bir giriş açardı.
        var results = await _executor.ExecuteAsync(
            (session, ct) => _apiClient.GetBusLocationsAsync(
                session,
                query: trimmed,
                marketLocale: marketLocale,
                cancellationToken: ct),
            cancellationToken);

        // API anlamsız terimlerde boş liste yerine popüler lokasyonlara
        // düşüyor; ilişkisiz doldurma kayıtları burada ayıklanıyor.
        return LocationRelevance.Filter(results, trimmed);
    }

    public Task<IReadOnlyList<BusLocation>> GetDefaultAsync(
        CancellationToken cancellationToken = default)
    {
        // Market Locale doğrudan kullanıcı girdisinden değil, beyaz listeden
        // geçen çözümleyiciden gelir; bkz. docs/adr/0002.
        var marketLocale = _marketLocale.Resolve();

        // Önbellek anahtarının Market Locale içermesi zorunlu: aynı çağrı
        // farklı pazar kodunda farklı adlar döndürüyor.
        return _cache.GetOrCreateDefaultAsync(
            marketLocale,
            // Arama terimi verilmediğinde API varsayılan, sıralanmış listeyi döndürür.
            ct => _executor.ExecuteAsync(
                (session, innerCt) => _apiClient.GetBusLocationsAsync(
                    session,
                    query: null,
                    marketLocale: marketLocale,
                    cancellationToken: innerCt),
                ct),
            cancellationToken);
    }
}
