using System.Globalization;

namespace Obilet.Web.Formatting;

/// <summary>
/// Olanak ikonlarının adreslerini üretir.
/// </summary>
/// <remarks>
/// Adres kalıbı obilet API dokümanında entegratörlerin kullanması için
/// açıkça belgelenmiştir; ikonlar API yanıtında gelmez, bu kalıpla çözülür.
/// Firma logosunda olduğu gibi her kimlik için ikon bulunduğu garanti değil:
/// olmayan bir kimlik <b>403</b> döndürüyor, 404 değil. Bu yüzden eksik ikon
/// durum koduna bakılarak değil, tarayıcının hata olayıyla ele alınır.
/// </remarks>
public static class FeatureIcon
{
    private const string UrlPattern =
        "https://s3.eu-central-1.amazonaws.com/static.obilet.com/images/feature/{0}.svg";

    /// <summary>
    /// Verilen olanak kimliği için ikon adresini döndürür.
    /// </summary>
    public static string UrlFor(int featureId) =>
        string.Format(CultureInfo.InvariantCulture, UrlPattern, featureId);
}
