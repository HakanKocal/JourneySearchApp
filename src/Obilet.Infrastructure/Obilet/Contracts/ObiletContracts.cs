namespace Obilet.Infrastructure.Obilet.Contracts;

// obilet API'sinin tel üzerindeki (wire) sözleşmeleri. Bu tipler yalnızca
// Infrastructure katmanında yaşar; uygulama katmanı kendi alan modellerini
// kullanır. Alan adları kebab-case'e serileştirme politikasıyla çevrilir,
// bu yüzden burada tek tek JsonPropertyName gerekmez.

/// <summary>
/// API'nin tüm yanıtlarını saran zarf.
/// </summary>
/// <remarks>
/// Başarı HTTP durum kodundan değil, <see cref="Status"/> alanından anlaşılır:
/// API başarısızlıkları çoğunlukla HTTP 200 ile döner.
/// </remarks>
internal sealed class ObiletResponse<TData>
{
    public string? Status { get; init; }
    public TData? Data { get; init; }

    /// <summary>
    /// Hata açıklaması. API burada kendi sunucu tarafı yığın izini
    /// döndürebilir, bu yüzden yalnızca loglanır.
    /// </summary>
    public string? Message { get; init; }

    public string? UserMessage { get; init; }
    public string? ApiRequestId { get; init; }
    public string? Controller { get; init; }
    public string? CorrelationId { get; init; }
}

/// <summary>
/// Oturum oluşturma isteği. Diğer isteklerin aksine Device Session taşımaz.
/// </summary>
internal sealed class SessionRequest
{
    public int Type { get; init; }
    public ConnectionInfo Connection { get; init; } = new();
    public BrowserInfo Browser { get; init; } = new();

    internal sealed class ConnectionInfo
    {
        public string IpAddress { get; init; } = string.Empty;
        public string Port { get; init; } = string.Empty;
    }

    internal sealed class BrowserInfo
    {
        public string Name { get; init; } = string.Empty;
        public string Version { get; init; } = string.Empty;
    }
}

/// <summary>Oturum oluşturma yanıtının veri kısmı.</summary>
internal sealed class DeviceSessionPayload
{
    public string? SessionId { get; init; }
    public string? DeviceId { get; init; }
}

/// <summary>
/// Oturum gerektiren tüm isteklerin ortak gövdesi.
/// </summary>
/// <typeparam name="TData">
/// Uç noktaya özel yük. Bus Location aramasında arama terimi (veya
/// <c>null</c>), sefer aramasında bir nesnedir.
/// </typeparam>
internal sealed class ObiletRequest<TData>
{
    public TData? Data { get; init; }
    public DeviceSessionPayload DeviceSession { get; init; } = new();

    /// <summary>İsteğin yapıldığı an. <c>yyyy-MM-ddTHH:mm:ss</c> biçiminde.</summary>
    public string Date { get; init; } = string.Empty;

    /// <summary>
    /// Market Locale. Bu bir görüntü dili değil, pazar seçicidir.
    /// </summary>
    public string Language { get; init; } = string.Empty;
}

/// <summary>Bus Location yanıt kaydı. Yalnızca kullandığımız alanlar tanımlıdır.</summary>
internal sealed class BusLocationPayload
{
    public int Id { get; init; }
    public string? Name { get; init; }
    public int? Rank { get; init; }
    public string? Keywords { get; init; }
}

/// <summary>Sefer aramasının uç noktaya özel yükü.</summary>
internal sealed class JourneyQueryPayload
{
    public int OriginId { get; init; }
    public int DestinationId { get; init; }

    /// <summary>Aranan kalkış günü. <c>yyyy-MM-ddTHH:mm:ss</c> biçiminde.</summary>
    public string DepartureDate { get; init; } = string.Empty;
}

/// <summary>
/// Sefer yanıt kaydı. Yalnızca kullandığımız alanlar tanımlıdır.
/// </summary>
/// <remarks>
/// API sefer başına yüzden fazla alan döndürüyor. Hepsini modellemek, hiçbiri
/// kullanılmayacakken bakım yükü ve gereksiz ayrıştırma maliyeti getirirdi;
/// tanımlanmayan alanlar sessizce yok sayılır.
/// </remarks>
internal sealed class BusJourneyPayload
{
    public long Id { get; init; }
    public int PartnerId { get; init; }
    public string? PartnerName { get; init; }
    public string? BusType { get; init; }
    public int TotalSeats { get; init; }
    public int AvailableSeats { get; init; }

    /// <summary>Seferin zaman, güzergâh ve fiyat bilgilerini taşıyan alt nesne.</summary>
    public JourneyDetailPayload? Journey { get; init; }

    internal sealed class JourneyDetailPayload
    {
        /// <summary>Kalkış terminalinin adı.</summary>
        public string? Origin { get; init; }

        /// <summary>Varış terminalinin adı.</summary>
        public string? Destination { get; init; }

        public DateTime Departure { get; init; }
        public DateTime Arrival { get; init; }

        /// <summary><c>HH:mm:ss</c> biçiminde yolculuk süresi.</summary>
        public TimeSpan? Duration { get; init; }

        public string? Currency { get; init; }

        /// <summary>İndirim öncesi liste fiyatı.</summary>
        public decimal OriginalPrice { get; init; }

        /// <summary>Çevrimiçi satış fiyatı.</summary>
        public decimal InternetPrice { get; init; }
    }
}
