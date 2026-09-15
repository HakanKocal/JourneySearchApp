using System.Net;

namespace Obilet.Application.Exceptions;

/// <summary>
/// obilet API'si bir isteği başarısız olarak yanıtladığında fırlatılır.
/// </summary>
/// <remarks>
/// API başarısızlıkları çoğunlukla HTTP 200 ile döner; başarı yalnızca yanıt
/// gövdesindeki <c>status</c> alanından anlaşılır. Bu yüzden durum yorumlaması
/// tek bir yerde, API istemcisinin içinde yapılır ve sonucu bu istisnadır.
///
/// API ayrıca başarısızlıkta kendi sunucu tarafı yığın izini <c>message</c>
/// alanında döndürür. <see cref="UpstreamMessage"/> yalnızca loglanmak içindir;
/// hiçbir koşulda kullanıcıya gösterilmemelidir.
/// </remarks>
public sealed class ObiletApiException : Exception
{
    public ObiletApiException(
        string status,
        string endpoint,
        string? upstreamMessage,
        string? correlationId,
        HttpStatusCode? httpStatusCode = null,
        TimeSpan? retryAfter = null)
        : base($"obilet API '{endpoint}' isteğini '{status}' durumuyla yanıtladı.")
    {
        Status = status;
        Endpoint = endpoint;
        UpstreamMessage = upstreamMessage;
        CorrelationId = correlationId;
        HttpStatusCode = httpStatusCode;
        RetryAfter = retryAfter;
    }

    /// <summary>API'nin döndürdüğü durum değeri (örneğin <c>InvalidRoute</c>).</summary>
    public string Status { get; }

    /// <summary>İsteğin yapıldığı API uç noktası.</summary>
    public string Endpoint { get; }

    /// <summary>
    /// API'nin hata açıklaması. Yığın izi içerebileceği için yalnızca loglanır.
    /// </summary>
    public string? UpstreamMessage { get; }

    /// <summary>API tarafındaki isteği izlemeye yarayan ilişkilendirme kimliği.</summary>
    public string? CorrelationId { get; }

    /// <summary>Yanıtın HTTP durum kodu, biliniyorsa.</summary>
    public HttpStatusCode? HttpStatusCode { get; }

    /// <summary>
    /// Hız sınırı yanıtında API'nin bildirdiği bekleme süresi, varsa.
    /// </summary>
    public TimeSpan? RetryAfter { get; }

    /// <summary>
    /// Device Session'ın artık geçerli olmadığını belirten durum. Bu durumda
    /// oturum yenilenip istek bir kez tekrarlanır.
    /// </summary>
    public const string DeviceSessionError = "DeviceSessionError";

    /// <summary>
    /// Bu istisnanın, oturum yenilenerek kurtarılabilir olup olmadığını söyler.
    /// </summary>
    public bool IsRecoverableBySessionRefresh =>
        string.Equals(Status, DeviceSessionError, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// İsteğin hız sınırı nedeniyle reddedilip reddedilmediğini söyler.
    /// </summary>
    /// <remarks>
    /// <para>
    /// API bir CDN arkasında ve oturum oluşturma çağrısı hız sınırlı. Sınır
    /// aşıldığında <c>HTTP 429</c> ve yaklaşık bir saatlik bir
    /// <c>Retry-After</c> dönüyor; gövde de JSON değil, düz metin bir hata
    /// kodu. Bu durum diğer arızalardan ayrılıyor, çünkü kullanıcıya
    /// söylenecek şey farklı: "bir hata oluştu" değil, "şu an çok fazla
    /// istek var, sonra tekrar deneyin".
    /// </para>
    /// <para>
    /// Yeniden denemek durumu kötüleştireceği için burada otomatik bir
    /// yeniden deneme yapılmıyor.
    /// </para>
    /// </remarks>
    public bool IsRateLimited =>
        HttpStatusCode == System.Net.HttpStatusCode.TooManyRequests
        || string.Equals(Status, "HTTP429", StringComparison.OrdinalIgnoreCase);
}
