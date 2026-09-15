using Obilet.Application.Abstractions;
using Obilet.Application.Models;

namespace Obilet.Tests.Fakes;

/// <summary>
/// API istemcisinin test karşılığı. Kaç kez oturum oluşturulduğunu sayar.
/// </summary>
internal sealed class FakeObiletApiClient : IObiletApiClient
{
    /// <summary><see cref="CreateSessionAsync"/> kaç kez çağrıldı.</summary>
    public int CreateSessionCallCount { get; private set; }

    public Task<DeviceSession> CreateSessionAsync(CancellationToken cancellationToken = default)
    {
        CreateSessionCallCount++;

        // Her çağrıda farklı bir çift döndürülür; böylece testler yenilenen
        // oturumun gerçekten yeni olduğunu doğrulayabilir.
        return Task.FromResult(new DeviceSession(
            $"session-{CreateSessionCallCount}",
            $"device-{CreateSessionCallCount}"));
    }

    public Task<IReadOnlyList<BusLocation>> GetBusLocationsAsync(
        DeviceSession deviceSession,
        string? query,
        string marketLocale,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<BusLocation>>([]);
}
