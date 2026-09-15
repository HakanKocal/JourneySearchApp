using Microsoft.AspNetCore.Mvc;
using Obilet.Application.Locations;

namespace Obilet.Web.Controllers;

/// <summary>
/// Arama formunun otomatik tamamlaması için Bus Location arar.
/// </summary>
/// <remarks>
/// <para>
/// Bu uç nokta, şartnamenin "istemci tarafından obilet API'sine doğrudan
/// istek yapılmamalı" kuralını karşılamak için var: tarayıcı yalnızca bu
/// adrese gidiyor, obilet API'sine giden çağrıyı uygulama backend'i yapıyor.
/// Böylece ApiClientToken ve Device Session tarayıcıya hiç ulaşmıyor.
/// </para>
/// <para>
/// Yanıt bilinçli olarak dar: yalnızca kimlik ve görünen ad. API'nin
/// döndürdüğü anahtar kelimeler, koordinatlar ve diğer alanlar arayüzde
/// kullanılmıyor ve dışarı verilmesi gereksiz.
/// </para>
/// </remarks>
[ApiController]
[Route("api/locations")]
public sealed class LocationsController : ControllerBase
{
    private readonly ILocationService _locationService;

    public LocationsController(ILocationService locationService)
    {
        _locationService = locationService;
    }

    /// <summary>
    /// Arama terimiyle ilişkili Bus Location'ları döndürür.
    /// </summary>
    /// <param name="q">Arama terimi.</param>
    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable<LocationSuggestion>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] string? q,
        CancellationToken cancellationToken)
    {
        var locations = await _locationService.SearchAsync(q, cancellationToken);

        // Sonuç bulunamaması bir hata değil: boş dizi döner ve arayüz bunu
        // "sonuç bulunamadı" olarak sunar.
        return Ok(locations.Select(location =>
            new LocationSuggestion(location.Id, location.Name)));
    }

    /// <summary>
    /// Otomatik tamamlama önerisi.
    /// </summary>
    /// <param name="Id">Bus Location kimliği.</param>
    /// <param name="Name">Kullanıcıya gösterilecek ad.</param>
    public sealed record LocationSuggestion(int Id, string Name);
}
