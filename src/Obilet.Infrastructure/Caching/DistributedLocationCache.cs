using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Obilet.Application.Caching;
using Obilet.Application.Models;

namespace Obilet.Infrastructure.Caching;

/// <inheritdoc cref="ILocationCache"/>
/// <remarks>
/// <see cref="IDistributedCache"/> üzerine kuruludur, somut bir sağlayıcıya
/// bağlı değildir: Redis yapılandırıldığında Redis, yapılandırılmadığında
/// süreç içi bellek kullanılır ve bu sınıf ikisini de bilmez. Bkz. docs/adr/0003.
/// </remarks>
public sealed class DistributedLocationCache : ILocationCache
{
    /// <summary>
    /// Lokasyon listesi nadiren değişir; yarım saat, API yükünü anlamlı
    /// ölçüde azaltırken verinin bayatlamasına da izin vermez.
    /// </summary>
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    private readonly IDistributedCache _cache;
    private readonly ILogger<DistributedLocationCache> _logger;

    public DistributedLocationCache(
        IDistributedCache cache,
        ILogger<DistributedLocationCache> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<IReadOnlyList<BusLocation>> GetOrCreateDefaultAsync(
        string marketLocale,
        Func<CancellationToken, Task<IReadOnlyList<BusLocation>>> factory,
        CancellationToken cancellationToken = default)
    {
        var key = BuildKey(marketLocale);

        var cached = await TryReadAsync(key, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var locations = await factory(cancellationToken);

        // Boş sonucu önbelleğe almıyoruz: geçici bir API sorunu yarım saat
        // boyunca boş bir lokasyon listesine dönüşmemeli.
        if (locations.Count > 0)
        {
            await TryWriteAsync(key, locations, cancellationToken);
        }

        return locations;
    }

    /// <summary>
    /// Önbellek anahtarı. Market Locale anahtarın parçasıdır; bkz.
    /// <see cref="ILocationCache"/> açıklaması.
    /// </summary>
    private static string BuildKey(string marketLocale) =>
        $"obilet:locations:default:{marketLocale}";

    private async Task<IReadOnlyList<BusLocation>?> TryReadAsync(
        string key,
        CancellationToken cancellationToken)
    {
        try
        {
            var bytes = await _cache.GetAsync(key, cancellationToken);
            if (bytes is null or { Length: 0 })
            {
                return null;
            }

            return JsonSerializer.Deserialize<List<BusLocation>>(bytes);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Önbellek bir kolaylıktır, bir bağımlılık değil. Redis'e
            // ulaşılamıyorsa veya giriş bozuksa uygulama çalışmaya devam
            // etmeli; yalnızca API'ye bir istek daha gider.
            _logger.LogWarning(ex, "Lokasyon önbelleği okunamadı, API'ye gidilecek.");
            return null;
        }
    }

    private async Task TryWriteAsync(
        string key,
        IReadOnlyList<BusLocation> locations,
        CancellationToken cancellationToken)
    {
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(locations);

            await _cache.SetAsync(
                key,
                bytes,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheDuration },
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Lokasyon önbelleğine yazılamadı.");
        }
    }
}
