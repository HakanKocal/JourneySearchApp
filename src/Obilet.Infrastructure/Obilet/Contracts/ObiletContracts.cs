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
