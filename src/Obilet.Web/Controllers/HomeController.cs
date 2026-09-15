using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Obilet.Application.Journeys;
using Obilet.Application.Locations;
using Obilet.Application.Time;
using Obilet.Web.Models;
using Obilet.Web.Validation;

namespace Obilet.Web.Controllers;

/// <summary>
/// Arama sayfasını sunar ve aramayı sefer sayfasına yönlendirir.
/// </summary>
public sealed class HomeController : Controller
{
    /// <summary>
    /// Sefer sayfasından yönlendirilen doğrulama hatasının taşındığı anahtar.
    /// </summary>
    public const string SearchErrorTempDataKey = "SearchErrorKeys";

    private readonly ILocationService _locationService;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly IMarketClock _clock;

    public HomeController(
        ILocationService locationService,
        IStringLocalizer<SharedResource> localizer,
        IMarketClock clock)
    {
        _locationService = locationService;
        _localizer = localizer;
        _clock = clock;
    }

    /// <summary>
    /// Arama formunu gösterir.
    /// </summary>
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var locations = await _locationService.GetDefaultAsync(cancellationToken);
        var model = JourneySearchViewModel.CreateDefault(locations, Today());

        // Sefer sayfasının adresi elle yazılıp doğrulamaya takıldıysa, hata
        // buraya taşınmış olur ve formun yanında gösterilir.
        RestoreErrorsFromTempData();

        return View(model);
    }

    /// <summary>
    /// Aramayı alır, doğrular ve kullanıcıyı sefer sayfasına yönlendirir.
    /// </summary>
    /// <remarks>
    /// Doğrulama geçerse sonuç sayfasına <see cref="RedirectToAction"/> ile
    /// gidilir. Böylece sefer sayfası kendi adresine sahip olur: yenilenebilir,
    /// paylaşılabilir ve geri tuşu beklendiği gibi çalışır. Sonucu doğrudan
    /// POST yanıtında render etmek bunların üçünü de bozardı.
    /// </remarks>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Search(
        JourneySearchViewModel form,
        CancellationToken cancellationToken)
    {
        var errors = SearchQueryValidator.Validate(
            form.OriginId, form.DestinationId, form.DepartureDate, Today());

        if (errors.Count > 0)
        {
            // Formu hatalarıyla birlikte yeniden gösteriyoruz. Lokasyon
            // listesi POST gövdesinde taşınmadığı için yeniden yüklenir;
            // bu çağrı önbelleklidir, ek API isteği getirmez.
            AddModelErrors(errors);

            var locations = await _locationService.GetDefaultAsync(cancellationToken);

            return View(nameof(Index), new JourneySearchViewModel
            {
                Locations = locations,
                Today = Today(),
                OriginId = form.OriginId,
                DestinationId = form.DestinationId,
                DepartureDate = form.DepartureDate,
            });
        }

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

    /// <summary>
    /// Kural ihlallerini ilgili form alanlarına bağlar.
    /// </summary>
    private void AddModelErrors(IEnumerable<SearchQueryError> errors)
    {
        foreach (var error in errors)
        {
            ModelState.AddModelError(
                SearchQueryErrorMessages.FieldFor(error),
                _localizer[SearchQueryErrorMessages.ResourceKeyFor(error)]);
        }
    }

    /// <summary>
    /// Sefer sayfasından taşınan hata anahtarlarını forma bağlar.
    /// </summary>
    /// <remarks>
    /// Taşınan şey metin değil <b>anahtar</b>: kullanıcı iki istek arasında
    /// dil değiştirirse mesaj yine doğru dilde görünsün.
    /// </remarks>
    private void RestoreErrorsFromTempData()
    {
        if (TempData[SearchErrorTempDataKey] is not string packedKeys
            || string.IsNullOrWhiteSpace(packedKeys))
        {
            return;
        }

        foreach (var name in packedKeys.Split(',', StringSplitOptions.RemoveEmptyEntries))
        {
            if (Enum.TryParse<SearchQueryError>(name, out var error))
            {
                ModelState.AddModelError(
                    SearchQueryErrorMessages.FieldFor(error),
                    _localizer[SearchQueryErrorMessages.ResourceKeyFor(error)]);
            }
        }
    }

    /// <remarks>
    /// Bugün, sunucunun değil <b>pazarın</b> saat diliminden okunur.
    /// Konteyner UTC çalışıyor ve pazar UTC+3; host saat dilimine güvenmek
    /// her gece üç saat boyunca yanlış bir "bugün" üretiyordu.
    /// Bkz. <see cref="IMarketClock"/>.
    /// </remarks>
    private DateOnly Today() => _clock.Today;
}
