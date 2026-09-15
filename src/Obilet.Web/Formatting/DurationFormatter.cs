using System.Globalization;

namespace Obilet.Web.Formatting;

/// <summary>
/// Yolculuk sürelerini biçimlendirir.
/// </summary>
/// <remarks>
/// <para>
/// Bu tipin var olma sebebi somut bir hata: süre <c>ToString(@"h\:mm")</c>
/// ile biçimlendirilmişti ve <c>h</c> saatin <b>gün dışında kalan</b>
/// bileşenidir (0–23). Ölçülen sonuç: 25 saat 30 dakikalık bir sefer
/// <c>1:30</c> olarak görünüyordu — gün bileşeni sessizce düşüyor ve
/// kullanıcıya bir günlük yolculuk bir buçuk saat gibi gösteriliyordu.
/// </para>
/// <para>
/// Doğu hatlarında 24 saati aşan seferler gerçekten var, dolayısıyla bu
/// teorik bir durum değil.
/// </para>
/// </remarks>
public static class DurationFormatter
{
    /// <summary>
    /// Süreyi <c>saat:dakika</c> olarak biçimlendirir; gün bileşeni toplam
    /// saate katılır.
    /// </summary>
    /// <returns>
    /// Süre yoksa <c>null</c>; çağıran bir yer tutucu gösterebilir.
    /// </returns>
    public static string? Format(TimeSpan? duration)
    {
        if (duration is null)
        {
            return null;
        }

        var value = duration.Value;

        // Negatif süre anlamsız; API'den gelirse gösterilmez.
        if (value < TimeSpan.Zero)
        {
            return null;
        }

        var totalHours = (int)value.TotalHours;

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{totalHours}:{value.Minutes:00}");
    }
}
