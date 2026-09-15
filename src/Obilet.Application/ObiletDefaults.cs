namespace Obilet.Application;

/// <summary>
/// Uygulama genelinde paylaşılan sabitler.
/// </summary>
public static class ObiletDefaults
{
    /// <summary>
    /// API'ye gönderilecek varsayılan Market Locale.
    /// </summary>
    /// <remarks>
    /// Market Locale bir görüntü dili değil, pazar seçicidir: farklı bir değer
    /// başka bir ülkenin lokasyon kataloğunu döndürür. Ayrıca API'nin resmî
    /// dokümanında varsayılan olarak verilen <c>en-EN</c> değeri isteği süresiz
    /// askıda bırakır. Bu nedenle bu değer hiçbir zaman doğrudan kullanıcı
    /// girdisinden gelmez. Ayrıntı ve beyaz liste için bkz. docs/adr/0002.
    /// </remarks>
    public const string MarketLocale = "tr-TR";
}
