using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Obilet.Application.Abstractions;
using Obilet.Application.Exceptions;
using Obilet.Application.Localization;
using Obilet.Application.Models;
using Obilet.Application.Time;
using Obilet.Infrastructure.Obilet.Contracts;

namespace Obilet.Infrastructure.Obilet;

/// <inheritdoc cref="IObiletApiClient"/>
public sealed class ObiletApiClient : IObiletApiClient
{
    private const string SuccessStatus = "Success";

    private const string SessionEndpoint = "client/getsession";
    private const string BusLocationsEndpoint = "location/getbuslocations";
    private const string BusJourneysEndpoint = "journey/getbusjourneys";

    private readonly HttpClient _httpClient;
    private readonly ObiletApiOptions _options;
    private readonly IMarketClock _clock;
    private readonly ILogger<ObiletApiClient> _logger;

    public ObiletApiClient(
        HttpClient httpClient,
        IOptions<ObiletApiOptions> options,
        IMarketClock clock,
        ILogger<ObiletApiClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _clock = clock;
        _logger = logger;
    }

    public async Task<DeviceSession> CreateSessionAsync(
        CancellationToken cancellationToken = default)
    {
        // Resmî dokümandaki gövde (type: 7 + application nesnesi) API tarafından
        // reddediliyor: tarayıcı tipli istemciler için port ve browser alanları
        // zorunlu tutuluyor. Buradaki gövde, çalıştığı doğrulanmış olan örnek
        // Postman koleksiyonundaki şekildir.
        var request = new SessionRequest
        {
            Type = _options.DeviceType,
            Connection = new SessionRequest.ConnectionInfo
            {
                IpAddress = _options.OutboundIpAddress,
                Port = _options.OutboundPort,
            },
            Browser = new SessionRequest.BrowserInfo
            {
                Name = _options.BrowserName,
                Version = _options.BrowserVersion,
            },
        };

        var payload = await PostAsync<SessionRequest, DeviceSessionPayload>(
            SessionEndpoint, request, cancellationToken);

        var session = new DeviceSession(
            payload?.SessionId ?? string.Empty,
            payload?.DeviceId ?? string.Empty);

        if (!session.IsUsable)
        {
            // Durum "Success" geldiği hâlde kimlik çifti boş: sözleşme
            // beklediğimiz gibi davranmıyor ve devam etmek anlamsız.
            throw new ObiletApiException(
                status: SuccessStatus,
                endpoint: SessionEndpoint,
                upstreamMessage: "Yanıt başarılı, ancak Device Session alanları boş döndü.",
                correlationId: null);
        }

        return session;
    }

    public async Task<IReadOnlyList<BusLocation>> GetBusLocationsAsync(
        DeviceSession deviceSession,
        string? query,
        string marketLocale,
        CancellationToken cancellationToken = default)
    {
        var request = BuildRequest(deviceSession, query, marketLocale);

        var payload = await PostAsync<ObiletRequest<string?>, List<BusLocationPayload>>(
            BusLocationsEndpoint, request, cancellationToken);

        if (payload is null)
        {
            return [];
        }

        // API'nin verdiği sıra korunur: varsayılan Origin ve Destination bu
        // sıranın ilk iki kaydından belirlenir, bu yüzden yeniden sıralamıyoruz.
        return payload
            .Where(item => !string.IsNullOrWhiteSpace(item.Name))
            .Select(item => new BusLocation(item.Id, item.Name!, item.Rank, item.Keywords))
            .ToList();
    }

    public async Task<IReadOnlyList<Journey>> GetBusJourneysAsync(
        DeviceSession deviceSession,
        int originId,
        int destinationId,
        DateOnly departureDate,
        string marketLocale,
        CancellationToken cancellationToken = default)
    {
        var query = new JourneyQueryPayload
        {
            OriginId = originId,
            DestinationId = destinationId,

            // API tarih alanını gün başlangıcı olarak bekliyor; saat
            // bileşeni verilmediğinde sonuç değişiyor.
            DepartureDate = departureDate.ToString("yyyy-MM-dd") + "T00:00:00",
        };

        var request = BuildRequest(deviceSession, query, marketLocale);

        var payload = await PostAsync<ObiletRequest<JourneyQueryPayload>, List<BusJourneyPayload>>(
            BusJourneysEndpoint, request, cancellationToken);

        if (payload is null)
        {
            return [];
        }

        // Projeksiyon burada yapılır: API yanıtı sefer başına yüzden fazla
        // alan taşıyor ve popüler bir hatta birkaç megabayta ulaşıyor.
        // Arayüze yalnızca kullanılan alanlar geçer.
        return payload
            .Where(item => item.Journey is not null)
            .Select(ToJourney)
            .ToList();
    }

    private static Journey ToJourney(BusJourneyPayload payload)
    {
        var detail = payload.Journey!;

        return new Journey(
            Id: payload.Id,
            PartnerId: payload.PartnerId,
            PartnerName: payload.PartnerName ?? string.Empty,
            BusType: payload.BusType,
            TotalSeats: payload.TotalSeats,
            AvailableSeats: payload.AvailableSeats,
            OriginStation: detail.Origin,
            DestinationStation: detail.Destination,
            Departure: detail.Departure,
            Arrival: detail.Arrival,
            Duration: detail.Duration,
            OriginalPrice: detail.OriginalPrice,
            InternetPrice: detail.InternetPrice,
            Currency: detail.Currency);
    }

    /// <summary>
    /// Oturum gerektiren isteklerin ortak gövdesini kurar.
    /// </summary>
    /// <remarks>
    /// Market Locale burada bir kez daha beyaz listeden geçirilir. Bu ikinci
    /// savunma bilinçlidir: web katmanındaki kültür çözümlemesinde bir gedik
    /// açılırsa, tanınmayan bir değerin API'ye ulaşması isteği süresiz askıda
    /// bırakır. Tek satırlık maliyetle bütün bir arıza sınıfı kapanıyor.
    /// </remarks>
    private ObiletRequest<TData> BuildRequest<TData>(
        DeviceSession deviceSession,
        TData data,
        string marketLocale) => new()
        {
            Data = data,
            DeviceSession = new DeviceSessionPayload
            {
                SessionId = deviceSession.SessionId,
                DeviceId = deviceSession.DeviceId,
            },

            // İstek anı pazarın saatinden okunur. Sunucunun saat dilimi UTC
            // olabilir ve konteynerde öyle; host saatine güvenmek API'ye
            // saatlerce kaymış bir istek anı bildirmek olurdu.
            Date = _clock.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
            Language = MarketLocale.Normalize(marketLocale),
        };

    /// <summary>
    /// İsteği gönderir, yanıt zarfını yorumlar ve başarısızlıkta istisna fırlatır.
    /// </summary>
    /// <remarks>
    /// Durum yorumlaması bilinçli olarak yalnızca burada yapılır. API
    /// başarısızlıkları HTTP 200 ile de gelebildiği için çağrı noktalarının
    /// HTTP durum koduna güvenmesi hataya açıktır; bu metot her iki durumu da
    /// aynı şekilde ele alır ve dışarıya tek bir istisna tipi sunar.
    /// </remarks>
    private async Task<TPayload?> PostAsync<TRequest, TPayload>(
        string endpoint,
        TRequest request,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            endpoint, request, ObiletJson.Options, cancellationToken);

        // Gövde, HTTP durum kodu ne olursa olsun okunur: geçersiz oturum
        // durumu HTTP 400 ile birlikte gövdede anlamlı bir durum değeri
        // taşıyor ve yeniden deneme kararı buna bağlı.
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        // Hız sınırı yanıtında API bekleme süresini bildiriyor; loglanması ve
        // kullanıcıya doğru mesajın gösterilmesi için taşınıyor.
        var retryAfter = response.Headers.RetryAfter?.Delta;

        ObiletResponse<TPayload>? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<ObiletResponse<TPayload>>(
                body, ObiletJson.Options);
        }
        catch (JsonException ex)
        {
            // Gövdesi JSON olmayan yanıtlar gerçekten oluyor: hız sınırı
            // aşıldığında API'nin önündeki CDN düz metin bir hata kodu
            // döndürüyor. Ayrıştırma hatası uygulamayı düşürmemeli.
            _logger.LogError(
                ex,
                "obilet API '{Endpoint}' isteğine ayrıştırılamayan bir yanıt döndürdü. HTTP {StatusCode}. RetryAfter: {RetryAfter}.",
                endpoint,
                (int)response.StatusCode,
                retryAfter);

            throw new ObiletApiException(
                status: $"HTTP{(int)response.StatusCode}",
                endpoint: endpoint,
                upstreamMessage: Truncate(body),
                correlationId: null,
                httpStatusCode: response.StatusCode,
                retryAfter: retryAfter);
        }

        var status = envelope?.Status ?? $"HTTP{(int)response.StatusCode}";

        if (!string.Equals(status, SuccessStatus, StringComparison.OrdinalIgnoreCase))
        {
            // Upstream mesaj yığın izi içerebilir: loglanır, asla kullanıcıya
            // gösterilmez. İlişkilendirme kimliği API tarafında aramayı sağlar.
            _logger.LogError(
                "obilet API '{Endpoint}' isteğini '{Status}' durumuyla yanıtladı. CorrelationId: {CorrelationId}. Upstream: {UpstreamMessage}",
                endpoint,
                status,
                envelope?.CorrelationId,
                Truncate(envelope?.Message));

            throw new ObiletApiException(
                status,
                endpoint,
                envelope?.Message,
                envelope?.CorrelationId,
                httpStatusCode: response.StatusCode,
                retryAfter: retryAfter);
        }

        return envelope!.Data;
    }

    /// <summary>Log kayıtlarının yığın izleriyle şişmesini engeller.</summary>
    private static string? Truncate(string? value, int maxLength = 500) =>
        value is { Length: > 0 } && value.Length > maxLength
            ? value[..maxLength] + "…"
            : value;
}
