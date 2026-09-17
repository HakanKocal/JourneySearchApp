using System.Text.RegularExpressions;

namespace Obilet.Web.Formatting;

/// <summary>
/// Dışarıdan gelen renk değerlerini işaretlemeye yazmadan önce doğrular.
/// </summary>
/// <remarks>
/// <para>
/// Tanıtımlı olanakların zemin ve metin renkleri obilet API'sinden geliyor
/// ve doğrudan bir <c>style</c> özniteliğine yazılıyor. Doğrulanmamış bir
/// değer burada bir enjeksiyon yolu olurdu: API'nin döndürdüğü metin
/// özniteliğin dışına taşabilir veya başka bir bildirim ekleyebilir.
/// </para>
/// <para>
/// Bu yüzden yalnızca katı bir renk biçimi kabul edilir; tanınmayan her
/// değer yok sayılır ve öğe varsayılan görünümüyle çizilir. Renk
/// gösterilememesi kabul edilebilir, güvenilmeyen metni işaretlemeye
/// yazmak değil.
/// </para>
/// </remarks>
public static partial class CssColor
{
    /// <summary>
    /// Kabul edilen biçim: <c>#RGB</c>, <c>#RRGGBB</c> veya <c>#RRGGBBAA</c>.
    /// </summary>
    [GeneratedRegex(
        "^#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6}|[0-9a-fA-F]{8})$",
        RegexOptions.CultureInvariant)]
    private static partial Regex HexColor();

    /// <summary>
    /// Değer geçerli bir renk sabitiyse onu döndürür, aksi hâlde <c>null</c>.
    /// </summary>
    public static string? Sanitize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();

        return HexColor().IsMatch(trimmed) ? trimmed : null;
    }
}
