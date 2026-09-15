using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Obilet.Application.Localization;

namespace Obilet.Web.Controllers;

/// <summary>
/// Kullanıcının Display Culture tercihini kaydeder.
/// </summary>
public sealed class CultureController : Controller
{
    /// <summary>
    /// Seçilen dili çereze yazar ve kullanıcıyı geldiği sayfaya döndürür.
    /// </summary>
    /// <remarks>
    /// <para>
    /// İki koruma var. Birincisi, gelen değer doğrudan çereze yazılmaz;
    /// beyaz listeden geçirilir. Tanınmayan bir Market Locale değeri API'yi
    /// süresiz askıda bırakıyor ve onu kalıcı bir çereze yazmak sorunu
    /// kullanıcının sonraki her isteğine taşırdı. Bkz. docs/adr/0002.
    /// </para>
    /// <para>
    /// İkincisi, dönüş adresi <see cref="LocalRedirect"/> ile kullanılıyor;
    /// dışarıya yönlendirme (open redirect) mümkün değil.
    /// </para>
    /// <para>
    /// POST olması bilinçli: dil değiştirmek sunucu tarafında bir durum
    /// değişikliği yapıyor ve bağlantı önizlemesi gibi tetikleyicilerle
    /// kazara gerçekleşmemeli.
    /// </para>
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Set(string? culture, string? returnUrl)
    {
        // Beyaz liste, desteklenen kültürlerle aynı kümeyi üretir.
        var safeCulture = MarketLocale.Normalize(culture);

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(safeCulture)),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),

                // Dil tercihi, kullanıcının açık talebiyle kaydedilen işlevsel
                // bir çerez; onay bandı olmadan da tutulabilir.
                IsEssential = true,

                HttpOnly = true,
                SameSite = SameSiteMode.Lax,
            });

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction(nameof(HomeController.Index), "Home");
    }
}
