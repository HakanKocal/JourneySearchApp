using Obilet.Application.Models;

namespace Obilet.Web.Models;

/// <summary>
/// Arama formunun view model'i.
/// </summary>
/// <remarks>
/// Device Session bilinçli olarak burada yer almaz ve hiçbir view model'e
/// konulmamalıdır: bir kimlik bilgisidir ve yalnızca sunucu tarafında yaşar.
/// </remarks>
public sealed class JourneySearchViewModel
{
    /// <summary>Seçilen kalkış Bus Location'ının kimliği.</summary>
    public int OriginId { get; set; }

    /// <summary>Seçilen varış Bus Location'ının kimliği.</summary>
    public int DestinationId { get; set; }

    /// <summary>Seçilen kalkış günü.</summary>
    public DateOnly DepartureDate { get; set; }

    /// <summary>
    /// Açılır listelerde gösterilecek Bus Location'lar.
    /// </summary>
    /// <remarks>
    /// API'nin döndürdüğü sıra korunur; varsayılan seçimler bu sıranın ilk
    /// iki kaydından belirlenir. Bu liste sistemdeki tüm lokasyonları
    /// içermez; bkz. docs/adr/0004.
    /// </remarks>
    public IReadOnlyList<BusLocation> Locations { get; init; } = [];

    /// <summary>
    /// Bugünün tarihi; seçilebilecek en erken gün.
    /// </summary>
    /// <remarks>
    /// Tarih alanının <c>min</c> özniteliğini beslemek için view'a taşınır.
    /// Bu yalnızca bir kolaylıktır, koruma değil: aynı kural sunucu tarafında
    /// da uygulanır, çünkü sefer sayfası doğrudan adresle açılabiliyor.
    /// </remarks>
    public DateOnly Today { get; init; }

    /// <summary>
    /// Formun ilk gösteriminde kullanılacak varsayılanları üretir.
    /// </summary>
    /// <param name="locations">API sırasını koruyan Bus Location listesi.</param>
    /// <param name="today">Bugünün tarihi.</param>
    public static JourneySearchViewModel CreateDefault(
        IReadOnlyList<BusLocation> locations,
        DateOnly today) => new()
        {
            Locations = locations,
            Today = today,
            OriginId = locations.Count > 0 ? locations[0].Id : 0,
            DestinationId = locations.Count > 1 ? locations[1].Id : 0,

            // Şartname varsayılan kalkış tarihinin yarın olmasını istiyor.
            DepartureDate = today.AddDays(1),
        };
}
