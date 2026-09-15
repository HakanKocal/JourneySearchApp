using Microsoft.AspNetCore.Mvc;
using Obilet.Application.Locations;
using Obilet.Web.Models;

namespace Obilet.Web.Controllers;

/// <summary>
/// Arama sayfasını sunar.
/// </summary>
public sealed class HomeController : Controller
{
    private readonly ILocationService _locationService;

    public HomeController(ILocationService locationService)
    {
        _locationService = locationService;
    }

    /// <summary>
    /// Bus Location listesini gösterir.
    /// </summary>
    /// <remarks>
    /// Bu aşamada sayfa yalnızca API'den gerçek veri geldiğini kanıtlar:
    /// Device Session oluşturma, kimlik doğrulama, kebab-case sözleşmesi ve
    /// durum yorumlaması uçtan uca çalışıyor demektir. Arama formu, takas,
    /// tarih seçimi ve sefer listesi sonraki adımların işidir.
    /// </remarks>
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var locations = await _locationService.GetDefaultAsync(cancellationToken);

        return View(new LocationListViewModel(locations));
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel
    {
        RequestId = HttpContext.TraceIdentifier,
    });
}
