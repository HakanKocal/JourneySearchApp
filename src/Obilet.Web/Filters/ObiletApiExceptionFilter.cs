using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Obilet.Application.Exceptions;
using Obilet.Web.Models;

namespace Obilet.Web.Filters;

/// <summary>
/// obilet API hatalarını kullanıcıya gösterilebilir bir yanıta çevirir.
/// </summary>
/// <remarks>
/// <para>
/// Tek bir yerde durmasının sebebi: API başarısızlıkta kendi sunucu tarafı
/// yığın izini yanıt gövdesinde döndürüyor. Her çağrı noktasının bunu
/// ayrı ayrı gizlemeye çalışması hataya açık olurdu; burada upstream detayın
/// loglanıp asla render edilmemesi garanti altına alınıyor.
/// </para>
/// <para>
/// Hız sınırı diğer arızalardan ayrılıyor, çünkü kullanıcıya söylenecek şey
/// farklı: "bir hata oluştu" değil, "şu an çok fazla istek var".
/// </para>
/// </remarks>
public sealed class ObiletApiExceptionFilter : IExceptionFilter
{
    private const string ViewName = "ApiError";

    private readonly ILogger<ObiletApiExceptionFilter> _logger;

    public ObiletApiExceptionFilter(ILogger<ObiletApiExceptionFilter> logger)
    {
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        var referenceId = context.HttpContext.TraceIdentifier;

        var (kind, statusCode) = Classify(context.Exception);

        if (kind is null)
        {
            // obilet API'siyle ilgisi olmayan bir hata; genel hata
            // yönetimine bırakılır.
            return;
        }

        // Upstream detay ve ilişkilendirme kimliği yalnızca loglanır. API
        // başarısızlıkta kendi yığın izini döndürüyor ve bu metin ekrana
        // gelmemeli.
        if (context.Exception is ObiletApiException apiException)
        {
            _logger.LogError(
                apiException,
                "obilet API hatası kullanıcıya gösterildi. Endpoint: {Endpoint}, Status: {Status}, " +
                "HTTP: {HttpStatusCode}, RetryAfter: {RetryAfter}, CorrelationId: {CorrelationId}, Reference: {ReferenceId}",
                apiException.Endpoint,
                apiException.Status,
                apiException.HttpStatusCode,
                apiException.RetryAfter,
                apiException.CorrelationId,
                referenceId);
        }
        else
        {
            _logger.LogError(
                context.Exception,
                "obilet API'sine ulaşılamadı. Reference: {ReferenceId}",
                referenceId);
        }

        context.Result = IsJsonEndpoint(context)
            ? JsonResultFor(kind.Value, referenceId, statusCode)
            : ViewResultFor(kind.Value, referenceId, statusCode);

        context.ExceptionHandled = true;
    }

    /// <summary>
    /// İstisnayı kullanıcıya gösterilecek hata türüne ve HTTP durum koduna eşler.
    /// </summary>
    /// <remarks>
    /// Ağ hatası ve timeout da buraya dâhil. Bunlar bu uygulamada beklenen
    /// senaryolar: API tanımadığı bir Market Locale değerinde isteği süresiz
    /// askıda bırakıyor, bu yüzden istemciye açık bir timeout konuldu ve
    /// tetiklendiğinde kullanıcı ham bir hata değil dostane bir sayfa
    /// görmeli. Hepsi 5xx ailesine girer: hata bizim isteğimizde değil,
    /// bağlı olduğumuz serviste.
    /// </remarks>
    private static (ApiErrorKind? Kind, int StatusCode) Classify(Exception exception) => exception switch
    {
        ObiletApiException { IsRateLimited: true } =>
            (ApiErrorKind.RateLimited, StatusCodes.Status503ServiceUnavailable),

        ObiletApiException =>
            (ApiErrorKind.Unexpected, StatusCodes.Status502BadGateway),

        // HttpClient timeout'u TaskCanceledException olarak yüzeye çıkar.
        // İsteğin kullanıcı tarafından iptal edilmesiyle karışmaması için
        // iç istisnası kontrol edilir.
        TaskCanceledException { InnerException: TimeoutException } =>
            (ApiErrorKind.Unexpected, StatusCodes.Status504GatewayTimeout),

        TimeoutException =>
            (ApiErrorKind.Unexpected, StatusCodes.Status504GatewayTimeout),

        HttpRequestException =>
            (ApiErrorKind.Unexpected, StatusCodes.Status502BadGateway),

        _ => (null, StatusCodes.Status500InternalServerError),
    };

    /// <summary>
    /// İsteğin JSON bekleyen bir uç noktaya mı geldiğini söyler.
    /// </summary>
    /// <remarks>
    /// Otomatik tamamlama uç noktası JSON döndürüyor; oraya bir HTML hata
    /// sayfası göndermek istemcide ayrıştırma hatasına yol açardı.
    /// </remarks>
    private static bool IsJsonEndpoint(ExceptionContext context) =>
        context.ActionDescriptor is ControllerActionDescriptor descriptor
        && descriptor.ControllerTypeInfo.GetCustomAttributes(typeof(ApiControllerAttribute), inherit: true).Length > 0;

    private static IActionResult JsonResultFor(ApiErrorKind kind, string referenceId, int statusCode) =>
        new ObjectResult(new { error = kind.ToString(), reference = referenceId })
        {
            StatusCode = statusCode,
        };

    private static IActionResult ViewResultFor(ApiErrorKind kind, string referenceId, int statusCode)
    {
        var model = new ApiErrorViewModel(kind, referenceId);

        return new ViewResult
        {
            ViewName = ViewName,
            StatusCode = statusCode,
            ViewData = new ViewDataDictionary<ApiErrorViewModel>(
                new EmptyModelMetadataProvider(),
                new ModelStateDictionary())
            {
                Model = model,
            },
        };
    }
}
