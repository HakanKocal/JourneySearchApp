using System.Globalization;

namespace Obilet.Application.Localization;

/// <inheritdoc cref="IMarketLocaleResolver"/>
public sealed class MarketLocaleResolver : IMarketLocaleResolver
{
    /// <remarks>
    /// <see cref="CultureInfo.CurrentUICulture"/> okunur; bu değer istek
    /// başına lokalizasyon ara katmanı tarafından ayarlanır. Ambient bir
    /// değerdir, HTTP tipi değil — bu yüzden uygulama katmanında durabilir.
    /// </remarks>
    public string Resolve() => MarketLocale.FromCulture(CultureInfo.CurrentUICulture);
}
