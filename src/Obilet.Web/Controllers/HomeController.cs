using Microsoft.AspNetCore.Mvc;
using Obilet.Application.Locations;
using Obilet.Web.Models;

namespace Obilet.Web.Controllers;

/// <summary>
/// Arama sayfasını sunar ve aramayı sefer sayfasına yönlendirir.
/// </summary>
public sealed class HomeController : Controller
{
    private readonly ILocationService _locationService;
    private readonly TimeProvider _timeProvider;

    public HomeController(ILocationService locationService, TimeProvider timeProvider)
    {
        _locationService = locationService;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Arama formunu gösterir.
    /// </summary>
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var locations = await _locationService.GetDefaultAsync(cancellationToken);

        return View(JourneySearchViewModel.CreateDefault(locations, Today()));
    }

    /// <summary>
    /// Aramayı alır ve kullanıcıyı sefer sayfasına yönlendirir.
    /// </summary>
    /// <remarks>
    /// Form POST ediliyor, ancak sonuç sayfasına <see cref="RedirectToAction"/>
    /// ile gidiliyor. Böylece sefer sayfası kendi adresine sahip olur:
    /// yenilenebilir, paylaşılabilir ve geri tuşu beklendiği gibi çalışır.
    /// Sonucu doğrudan POST yanıtında render etmek bunların üçünü de bozardı.
    ///
    /// Doğrulama kuralları bu adımda henüz uygulanmıyor; sefer sayfası
    /// doğrudan adresle de açılabildiği için doğrulamanın her iki girişte
    /// birlikte ele alınması gerekiyor.
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Search(JourneySearchViewModel form)
    {
        return RedirectToAction(
            nameof(JourneyController.Index),
            "Journey",
            new
            {
                originId = form.OriginId,
                destinationId = form.DestinationId,
                date = form.DepartureDate.ToString("yyyy-MM-dd"),
            });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel
    {
        RequestId = HttpContext.TraceIdentifier,
    });

    /// <remarks>
    /// <see cref="TimeProvider"/> üzerinden okunur; <c>DateTime.Today</c>
    /// doğrudan kullanıldığında tarihe bağlı davranış test edilemez hâle gelir.
    /// </remarks>
    private DateOnly Today() =>
        DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);
}
