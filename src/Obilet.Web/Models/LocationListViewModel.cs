using Obilet.Application.Models;

namespace Obilet.Web.Models;

/// <summary>
/// Bus Location listesini görüntülemek için kullanılan view model.
/// </summary>
/// <remarks>
/// Device Session bilinçli olarak bu modelde yer almaz ve hiçbir view model'e
/// konulmamalıdır: bir kimlik bilgisidir ve yalnızca sunucu tarafında yaşar.
/// </remarks>
/// <param name="Locations">API'nin döndürdüğü sırayı koruyan liste.</param>
public sealed record LocationListViewModel(IReadOnlyList<BusLocation> Locations)
{
    /// <summary>
    /// Varsayılan Origin: API sıralamasının ilk kaydı.
    /// </summary>
    public BusLocation? DefaultOrigin => Locations.Count > 0 ? Locations[0] : null;

    /// <summary>
    /// Varsayılan Destination: API sıralamasının ikinci kaydı.
    /// </summary>
    public BusLocation? DefaultDestination => Locations.Count > 1 ? Locations[1] : null;
}
