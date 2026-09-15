using System.Text;

namespace Obilet.Application.Locations;

/// <summary>
/// Metinleri Türkçe'ye duyarlı arama karşılaştırması için normalleştirir.
/// </summary>
/// <remarks>
/// <para>
/// Türkçe'de <c>i</c> harfinin iki büyük hâli (<c>İ</c> ve <c>I</c>) ve iki
/// küçük hâli (<c>i</c> ve <c>ı</c>) var. Bu yüzden kültüre duyarlı
/// küçültme tek başına yetmiyor: <c>"İzmir".ToLower("tr-TR")</c> sonucu
/// <c>izmir</c>, <c>"Izmir".ToLower("tr-TR")</c> sonucu ise <c>ızmir</c>
/// olur ve ikisi eşleşmez. Yani noktasız <c>I</c> ile yazan kullanıcı
/// <c>İzmir</c>'i bulamaz.
/// </para>
/// <para>
/// Ayrıca kullanıcılar Türkçe karakterleri sık sık ASCII karşılıklarıyla
/// yazıyor: <c>sanliurfa</c> yazan biri <c>Şanlıurfa</c>'yı bulmayı bekler.
/// </para>
/// <para>
/// Bu iki sorunun ortak çözümü katlama (folding): tüm <c>i</c> varyantları
/// tek bir karaktere, Türkçe'ye özgü harfler de ASCII karşılıklarına
/// indirgenir. Sonuç yalnızca karşılaştırma içindir; kullanıcıya hiçbir
/// zaman gösterilmez.
/// </para>
/// </remarks>
public static class TurkishSearchText
{
    /// <summary>
    /// Metni arama karşılaştırmasına uygun hâle getirir.
    /// </summary>
    /// <remarks>
    /// Boş veya yalnızca boşluktan oluşan girdi boş dize döndürür.
    /// </remarks>
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        var previousWasSeparator = false;

        foreach (var character in value)
        {
            var folded = Fold(character);

            if (folded == ' ')
            {
                // Boşluklar tek boşluğa indirilir ve baştaki boşluk atılır;
                // "İstanbul   Avrupa" ile "istanbul avrupa" eşleşsin.
                if (builder.Length > 0 && !previousWasSeparator)
                {
                    builder.Append(' ');
                    previousWasSeparator = true;
                }

                continue;
            }

            if (folded != '\0')
            {
                builder.Append(folded);
                previousWasSeparator = false;
            }
        }

        // Sondaki boşluk varsa atılır.
        if (builder.Length > 0 && builder[^1] == ' ')
        {
            builder.Length--;
        }

        return builder.ToString();
    }

    /// <summary>
    /// İki metnin arama açısından ilişkili olup olmadığını söyler.
    /// </summary>
    /// <param name="haystack">İçinde aranan metin.</param>
    /// <param name="needle">Aranan terim.</param>
    public static bool Contains(string? haystack, string? needle)
    {
        var normalizedNeedle = Normalize(needle);

        if (normalizedNeedle.Length == 0)
        {
            return false;
        }

        return Normalize(haystack).Contains(normalizedNeedle, StringComparison.Ordinal);
    }

    /// <summary>
    /// Tek bir karakteri katlar. <c>'\0'</c> dönerse karakter atılır.
    /// </summary>
    private static char Fold(char character) => character switch
    {
        // i harfinin bütün varyantları tek karaktere indirilir. Türkçe'nin
        // noktalı/noktasız i ayrımı aramada kullanıcıya engel olmamalı.
        'İ' or 'I' or 'ı' or 'i' or 'Í' or 'í' or 'Î' or 'î' => 'i',

        'Ş' or 'ş' => 's',
        'Ğ' or 'ğ' => 'g',
        'Ç' or 'ç' => 'c',
        'Ö' or 'ö' => 'o',
        'Ü' or 'ü' => 'u',
        'Â' or 'â' => 'a',
        'Û' or 'û' => 'u',
        'Ê' or 'ê' => 'e',

        // Harf veya rakamsa küçültülür; kültüre duyarsız küçültme kullanılır
        // çünkü i varyantları yukarıda zaten ele alındı.
        _ when char.IsLetterOrDigit(character) => char.ToLowerInvariant(character),

        // Boşluk ve noktalama ayırıcı sayılır: "Ankara (Aşti)" ile
        // "ankara asti" eşleşsin.
        _ => ' ',
    };
}
