using Microsoft.AspNetCore.Http.Features;
using Obilet.Application.Abstractions;

namespace Obilet.Web.Sessions;

/// <summary>
/// Visitor Session deposunun ASP.NET Core oturumu üzerine kurulu uygulaması.
/// </summary>
/// <remarks>
/// Uygulama katmanının HTTP tiplerine bağımlı olmaması için soyutlamanın
/// somut hâli burada, web katmanında durur. Böylece Device Session
/// orkestrasyonu bir HTTP bağlamı kurmadan test edilebilir.
/// </remarks>
public sealed class HttpVisitorSessionStore : IVisitorSessionStore
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpVisitorSessionStore(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? Get(string key) => Session?.GetString(key);

    public void Set(string key, string value) => Session?.SetString(key, value);

    public void Remove(string key) => Session?.Remove(key);

    private ISession? Session
    {
        get
        {
            // Oturum ara katmanı çalışmadan erişilirse istisna fırlar. Bu
            // depo yalnızca istek boru hattı içinden kullanıldığı için
            // normal akışta null dönmez; savunma amaçlı kontrol edilir.
            var context = _httpContextAccessor.HttpContext;
            return context?.Features.Get<ISessionFeature>() is null ? null : context.Session;
        }
    }
}
