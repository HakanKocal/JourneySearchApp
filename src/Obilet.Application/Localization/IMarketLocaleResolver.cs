namespace Obilet.Application.Localization;

/// <summary>
/// Geçerli isteğin Display Culture değerinden, API'ye gönderilecek
/// Market Locale değerini üretir.
/// </summary>
/// <remarks>
/// İki kavram bilinçli olarak ayrıdır: Display Culture kullanıcının arayüz
/// tercihi, Market Locale ise API'nin pazar seçicisidir. Aradaki eşleme
/// kapalı bir beyaz listedir; bkz. <see cref="MarketLocale"/>.
/// </remarks>
public interface IMarketLocaleResolver
{
    /// <summary>
    /// Geçerli Display Culture için izin verilen Market Locale değerini döndürür.
    /// </summary>
    string Resolve();
}
