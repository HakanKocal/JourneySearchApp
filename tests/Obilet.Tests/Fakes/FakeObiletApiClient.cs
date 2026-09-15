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

    /// <summary>Lokasyon çağrısının döndüreceği liste.</summary>
    public IReadOnlyList<BusLocation> Locations { get; set; } = [];

    /// <summary>Lokasyon çağrısı kaç kez yapıldı.</summary>
    public int LocationCallCount { get; private set; }

    /// <summary>Lokasyon çağrısına geçirilen son arama terimi.</summary>
    public string? LastLocationQuery { get; private set; }

    public Task<IReadOnlyList<BusLocation>> GetBusLocationsAsync(
        DeviceSession deviceSession,
        string? query,
        string marketLocale,
        CancellationToken cancellationToken = default)
    {
        LocationCallCount++;
        LastLocationQuery = query;

        return Task.FromResult(Locations);
    }

    /// <summary>Sefer aramasının döndüreceği liste.</summary>
    public IReadOnlyList<Journey> Journeys { get; set; } = [];

    /// <summary>
    /// Ayarlandığında sefer aramasının bunun yerine fırlatacağı istisna.
    /// </summary>
    public Exception? JourneySearchException { get; set; }

    /// <summary>Sefer aramasına geçirilen son Market Locale değeri.</summary>
    public string? LastMarketLocale { get; private set; }

    public Task<IReadOnlyList<Journey>> GetBusJourneysAsync(
        DeviceSession deviceSession,
        int originId,
        int destinationId,
        DateOnly departureDate,
        string marketLocale,
        CancellationToken cancellationToken = default)
    {
        LastMarketLocale = marketLocale;

        if (JourneySearchException is not null)
        {
            throw JourneySearchException;
        }

        return Task.FromResult(Journeys);
    }
}
