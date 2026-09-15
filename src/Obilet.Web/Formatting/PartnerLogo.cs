using System.Globalization;

namespace Obilet.Web.Formatting;

/// <summary>
/// Otobüs firmalarının logo adreslerini üretir.
/// </summary>
/// <remarks>
/// Adres kalıbı obilet API dokümanında entegratörlerin kullanması için
/// açıkça belgelenmiştir; logolar API yanıtında gelmez, bu kalıpla çözülür.
/// Logo bir CDN üzerinde durduğu için her firma için mevcut olduğu garanti
/// değil; görünüm yüklenemeyen logoyu gizler ve firma adı yazıyla kalır.
/// </remarks>
public static class PartnerLogo
{
    private const string UrlPattern =
        "https://s3.eu-central-1.amazonaws.com/static.obilet.com/images/partner/{0}-sm.png";

    /// <summary>
    /// Verilen firma kimliği için logo adresini döndürür.
    /// </summary>
    public static string UrlFor(int partnerId) =>
        string.Format(CultureInfo.InvariantCulture, UrlPattern, partnerId);
}
