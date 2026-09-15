using Microsoft.Extensions.Caching.Distributed;

namespace Obilet.Tests.Fakes;

/// <summary>
/// Her işlemde hata veren dağıtık önbellek.
/// </summary>
/// <remarks>
/// Redis'e ulaşılamadığı durumu taklit eder. Önbellek bir kolaylık olduğu
/// için uygulamanın bu durumda da çalışmaya devam etmesi gerekir.
/// </remarks>
internal sealed class ThrowingDistributedCache : IDistributedCache
{
    private static InvalidOperationException Failure() => new("Önbelleğe ulaşılamıyor.");

    public byte[]? Get(string key) => throw Failure();

    public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        => throw Failure();

    public void Refresh(string key) => throw Failure();

    public Task RefreshAsync(string key, CancellationToken token = default)
        => throw Failure();

    public void Remove(string key) => throw Failure();

    public Task RemoveAsync(string key, CancellationToken token = default)
        => throw Failure();

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        => throw Failure();

    public Task SetAsync(
        string key,
        byte[] value,
        DistributedCacheEntryOptions options,
        CancellationToken token = default)
        => throw Failure();
}
