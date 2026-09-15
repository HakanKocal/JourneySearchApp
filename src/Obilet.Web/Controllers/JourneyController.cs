using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Obilet.Application.Journeys;
using Obilet.Application.Locations;
using Obilet.Web.Models;

namespace Obilet.Web.Controllers;

/// <summary>
/// Bir Search Query için uygun Journey listesini sunar.
/// </summary>
/// <remarks>
/// Adres kalıbı obilet'in kendi dokümante ettiği biçimi izler:
/// <c>/seferler/{origin-id}-{destination-id}/{YYYY-MM-DD}</c>. Böylece adres
/// paylaşılabilir ve yer imlenebilir olur.
/// </remarks>
[Route("seferler")]
public sealed class JourneyController : Controller
{
    /// <summary>Adresteki tarihin beklenen biçimi.</summary>
    private const string DateFormat = "yyyy-MM-dd";

    private readonly IJourneyService _journeyService;
    private readonly ILocationService _locationService;

    public JourneyController(
        IJourneyService journeyService,
        ILocationService locationService)
    {
        _journeyService = journeyService;
        _locationService = locationService;
    }

    /// <summary>
    /// Sefer listesini gösterir.
    /// </summary>
    [HttpGet("{originId:int}-{destinationId:int}/{date}")]
    public async Task<IActionResult> Index(
        int originId,
        int destinationId,
        string date,
        CancellationToken cancellationToken)
    {
        // Adres elle yazılabildiği için tarih biçimi garanti değil. Biçimi
        // bozuk bir adres hata sayfası yerine arama formuna döner: kullanıcı
        // için yapacak bir şey var ve bu, hata göstermekten daha yardımcı.
        if (!DateOnly.TryParseExact(
                date,
                DateFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var departureDate))
        {
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        var journeys = await _journeyService.SearchAsync(
            originId, destinationId, departureDate, cancellationToken);

        // Başlıkta lokasyon adlarını gösterebilmek için isimler çözülür.
        // Bu çağrı önbelleklidir, dolayısıyla ek bir API isteği getirmez.
        var locations = await _locationService.GetDefaultAsync(cancellationToken);

        return View(new JourneyListViewModel(
            Journeys: journeys,
            OriginId: originId,
            DestinationId: destinationId,
            DepartureDate: departureDate,
            OriginName: NameOf(locations, originId),
            DestinationName: NameOf(locations, destinationId)));
    }

    /// <remarks>
    /// Ad bulunamayabilir: varsayılan liste sistemdeki tüm lokasyonları
    /// içermiyor ve kullanıcı arama yoluyla listede olmayan bir lokasyon
    /// seçmiş olabilir. Bu durumda başlık kimliğe düşer, sayfa bozulmaz.
    /// </remarks>
    private static string? NameOf(
        IEnumerable<Application.Models.BusLocation> locations,
        int id) =>
        locations.FirstOrDefault(location => location.Id == id)?.Name;
}
