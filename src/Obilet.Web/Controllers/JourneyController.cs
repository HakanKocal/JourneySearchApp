using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Obilet.Application.Journeys;
using Obilet.Application.Locations;
using Obilet.Application.Time;
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
    private readonly IMarketClock _clock;

    public JourneyController(
        IJourneyService journeyService,
        ILocationService locationService,
        IMarketClock clock)
    {
        _journeyService = journeyService;
        _locationService = locationService;
        _clock = clock;
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

        // Bu sayfa doğrudan adresle açılabildiği için doğrulama burada da
        // uygulanır. Formu atlayıp geçmiş bir tarih yazan biri engellenmeli:
        // API bu kuralı uygulamıyor ve geçmiş tarihli sorguya sonuç döndürüyor.
        var errors = SearchQueryValidator.Validate(
            originId, destinationId, departureDate, Today());

        if (errors.Count > 0)
        {
            // Hatalar arama formuna taşınır; kullanıcı sorunu düzeltebileceği
            // yere gönderilir. Taşınan şey metin değil kural adı, böylece
            // mesaj isteği karşılayan kültürde üretilir.
            TempData[HomeController.SearchErrorTempDataKey] =
                string.Join(',', errors.Select(error => error.ToString()));

            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        var journeys = await _journeyService.SearchAsync(
            originId, destinationId, departureDate, cancellationToken);

        var (originName, destinationName) =
            await ResolveNamesAsync(journeys, originId, destinationId, cancellationToken);

        return View(new JourneyListViewModel(
            Journeys: journeys,
            OriginId: originId,
            DestinationId: destinationId,
            DepartureDate: departureDate,
            OriginName: originName,
            DestinationName: destinationName,
            Today: Today()));
    }

    /// <summary>
    /// Sorgulanan lokasyonların gösterilecek adlarını çözer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Asıl kaynak seferlerin kendisi: API her sefer kaydında kalkış ve varış
    /// <b>lokasyonunun</b> adını gönderiyor. Bu, bir hatayı düzeltiyor —
    /// adlar önceden yalnızca varsayılan 20 kayıtlık listeden çözülüyordu ve
    /// o listede olmayan bir lokasyon seçildiğinde ada değil kimliğe
    /// düşülüyordu: kullanıcı "Rize" yerine "400" görüyordu. Varsayılan
    /// listenin tüm lokasyonları içermemesi bir API kısıtı; bkz. docs/adr/0004.
    /// </para>
    /// <para>
    /// Varsayılan listeye yalnızca gerekli olduğunda bakılıyor: sonuç boşsa
    /// okunacak sefer kaydı da yok. O çağrı önbellekli, yani ek bir API
    /// isteği getirmiyor, ama gereksizken de yapılmasına gerek yok.
    /// </para>
    /// <para>
    /// Her iki kaynak da yetersiz kalırsa ad <c>null</c> döner ve arayüz
    /// kimliği gösterir. Bu yalnızca sefer bulunmayan bir sorguda, hem de
    /// varsayılan listede olmayan bir lokasyon için mümkün; API kimlikten ada
    /// çözüm yapan bir uç nokta sunmadığı için daha iyisi elde yok.
    /// </para>
    /// </remarks>
    private async Task<(string? Origin, string? Destination)> ResolveNamesAsync(
        IReadOnlyList<Application.Models.Journey> journeys,
        int originId,
        int destinationId,
        CancellationToken cancellationToken)
    {
        // Tüm kayıtlar aynı güzergâhı bildiriyor; ilki yeterli.
        var sample = journeys.Count > 0 ? journeys[0] : null;

        var origin = NullIfBlank(sample?.OriginLocation);
        var destination = NullIfBlank(sample?.DestinationLocation);

        if (origin is not null && destination is not null)
        {
            return (origin, destination);
        }

        var locations = await _locationService.GetDefaultAsync(cancellationToken);

        return (
            origin ?? NameOf(locations, originId),
            destination ?? NameOf(locations, destinationId));
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private static string? NameOf(
        IEnumerable<Application.Models.BusLocation> locations,
        int id) =>
        locations.FirstOrDefault(location => location.Id == id)?.Name;

    /// <remarks>
    /// Bugün, sunucunun değil <b>pazarın</b> saat diliminden okunur;
    /// bkz. <see cref="IMarketClock"/>.
    /// </remarks>
    private DateOnly Today() => _clock.Today;
}
