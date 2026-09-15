using Obilet.Application.Time;

namespace Obilet.Tests.Fakes;

/// <summary>
/// Sabit bir pazar saati döndüren saat.
/// </summary>
internal sealed class StubMarketClock : IMarketClock
{
    private readonly DateTimeOffset _now;

    public StubMarketClock(DateTimeOffset? now = null)
    {
        // Varsayılan: pazar saatiyle sıradan bir gün ortası.
        _now = now ?? new DateTimeOffset(2026, 9, 15, 12, 0, 0, TimeSpan.FromHours(3));
    }

    public DateTimeOffset Now => _now;

    public DateOnly Today => DateOnly.FromDateTime(_now.DateTime);
}
