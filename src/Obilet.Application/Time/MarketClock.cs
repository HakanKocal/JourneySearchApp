namespace Obilet.Application.Time;

/// <inheritdoc cref="IMarketClock"/>
public sealed class MarketClock : IMarketClock
{
    /// <summary>
    /// Pazarın saat dilimi için denenen kimlikler.
    /// </summary>
    /// <remarks>
    /// IANA kimliği önce denenir; Windows'ta ICU üzerinden çözülür ama
    /// çözülemediği ortamlar olabileceği için Windows kimliği de listede.
    /// obilet API'sinin lokasyon yanıtında bildirdiği <c>tz-code</c> değeri
    /// de <c>Turkey Standard Time</c>'dır.
    /// </remarks>
    private static readonly string[] TimeZoneIds =
    [
        "Europe/Istanbul",
        "Turkey Standard Time",
    ];

    /// <summary>
    /// Saat dilimi hiç çözülemezse kullanılan sabit fark.
    /// </summary>
    /// <remarks>
    /// Türkiye 2016'dan beri yaz saati uygulamıyor ve kalıcı olarak UTC+3.
    /// Yine de öncelik <see cref="TimeZoneInfo"/>'da: ileride yaz saati
    /// yeniden getirilirse sabit fark yanlış olurdu.
    /// </remarks>
    private static readonly TimeSpan FallbackOffset = TimeSpan.FromHours(3);

    private readonly TimeProvider _timeProvider;
    private readonly TimeZoneInfo? _marketTimeZone;

    public MarketClock(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
        _marketTimeZone = ResolveMarketTimeZone();
    }

    public DateTimeOffset Now
    {
        get
        {
            var utcNow = _timeProvider.GetUtcNow();

            return _marketTimeZone is null
                ? utcNow.ToOffset(FallbackOffset)
                : TimeZoneInfo.ConvertTime(utcNow, _marketTimeZone);
        }
    }

    public DateOnly Today => DateOnly.FromDateTime(Now.DateTime);

    private static TimeZoneInfo? ResolveMarketTimeZone()
    {
        foreach (var id in TimeZoneIds)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
            {
                // Sonraki kimlik denenir; hiçbiri çözülemezse sabit farka düşülür.
            }
        }

        return null;
    }
}
