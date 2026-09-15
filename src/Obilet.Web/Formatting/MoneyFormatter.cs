using System.Globalization;

namespace Obilet.Web.Formatting;

/// <summary>
/// Para tutarlarını biçimlendirir.
/// </summary>
/// <remarks>
/// <para>
/// Bu tipin var olma sebebi somut bir hata: <c>decimal.ToString("C2")</c>
/// ambient kültürün para birimi sembolünü kullanır. Arayüz İngilizceye
/// geçtiğinde <c>en-US</c> kültürü devreye girer ve Türk Lirası cinsinden
/// bir tutar <c>$900.00</c> olarak görünür. Kullanıcıya fiyatı yanlış
/// bildirmek, kabul edilebilir bir biçimlendirme farkı değil.
/// </para>
/// <para>
/// Doğru davranış ikiyi ayırmaktır: <b>sayı biçimi</b> kullanıcının
/// kültüründen gelir (ondalık ayırıcı, binlik gruplama), <b>para birimi</b>
/// ise API'nin o sefer için bildirdiği koddan. Böylece Türkçe arayüzde
/// <c>900,00 ₺</c>, İngilizce arayüzde <c>900.00 ₺</c> görünür — ikisi de
/// aynı parayı, doğru birimle ifade eder.
/// </para>
/// </remarks>
public static class MoneyFormatter
{
    /// <summary>
    /// Bilinen para birimi kodlarının gösterim sembolleri.
    /// </summary>
    /// <remarks>
    /// Listede olmayan bir kod sembole çevrilmez, kodun kendisi gösterilir.
    /// Tanımadığımız bir para birimini yanlış bir sembolle göstermektense
    /// ISO kodunu göstermek doğrudur.
    /// </remarks>
    private static readonly Dictionary<string, string> Symbols =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["TRY"] = "₺",
            ["EUR"] = "€",
            ["USD"] = "$",
            ["GBP"] = "£",
        };

    /// <summary>
    /// Tutarı, geçerli kültürün sayı biçimiyle ve verilen para biriminin
    /// sembolüyle biçimlendirir.
    /// </summary>
    /// <param name="amount">Biçimlendirilecek tutar.</param>
    /// <param name="currencyCode">ISO 4217 para birimi kodu.</param>
    public static string Format(decimal amount, string? currencyCode)
    {
        // Sayı biçimi kullanıcının kültüründen: tr-TR'de virgüllü ondalık,
        // en-US'te noktalı.
        var number = amount.ToString("N2", CultureInfo.CurrentCulture);

        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            return number;
        }

        var symbol = Symbols.TryGetValue(currencyCode, out var known)
            ? known
            : currencyCode;

        // Sembol sondadır; tasarım şartnamesindeki "75,00 TL" yerleşimini
        // ve Türkçe yazım alışkanlığını izler.
        return $"{number} {symbol}";
    }
}
