using System.Globalization;

namespace Obilet.Application.Localization;

/// <summary>
/// obilet API'sine gönderilebilecek Market Locale değerlerinin tek doğruluk kaynağı.
/// </summary>
/// <remarks>
/// <para>
/// Market Locale bir görüntü dili <b>değil</b>, pazar seçicidir. Canlı API
/// üzerinde doğrulanan davranış:
/// </para>
/// <list type="bullet">
///   <item><c>tr-TR</c> → Türk lokasyonları, Türkçe adlarla</item>
///   <item><c>en-US</c> → Türk lokasyonları, İngilizce adlarla</item>
///   <item><c>en-GB</c> → <b>Britanya</b> lokasyonları (London, Birmingham…)</item>
///   <item><c>de-DE</c>, <c>fr-FR</c>, <c>ru-RU</c> → ilgili ülkenin lokasyonları</item>
///   <item><c>en-EN</c> → <b>istek süresiz askıda kalıyor</b></item>
///   <item>Tanınmayan değerler → aynı şekilde askıda kalıyor</item>
/// </list>
/// <para>
/// Bu yüzden değer asla doğrudan kullanıcı girdisinden veya
/// <see cref="CultureInfo"/>'nun adından alınmaz; <see cref="Normalize"/>
/// üzerinden geçer. Aksi hâlde <c>?culture=xx-XX</c> gibi bir istek sunucu
/// thread'lerini tüketebilir. Ayrıntı için bkz. docs/adr/0002.
/// </para>
/// </remarks>
public static class MarketLocale
{
    /// <summary>Türk pazarı, Türkçe metinler.</summary>
    public const string Turkish = "tr-TR";

    /// <summary>
    /// Türk pazarı, İngilizce metinler.
    /// </summary>
    /// <remarks>
    /// İngilizce için <b>tek geçerli seçenek budur</b>. <c>en-GB</c>
    /// kullanıcıyı sessizce Britanya otobüs hatlarına düşürür.
    /// </remarks>
    public const string English = "en-US";

    /// <summary>Tanınmayan bir değer geldiğinde kullanılan güvenli varsayılan.</summary>
    public const string Default = Turkish;

    /// <summary>API'ye gönderilmesine izin verilen değerler.</summary>
    public static readonly IReadOnlyList<string> Supported = [Turkish, English];

    /// <summary>
    /// Resmî API dokümanının varsayılan olarak belgelediği, ancak isteği
    /// süresiz askıda bırakan değer. Yalnızca test ve belgeleme amaçlı durur.
    /// </summary>
    public const string DocumentedButBroken = "en-EN";

    /// <summary>
    /// Verilen değeri izin verilen bir Market Locale'e indirger.
    /// Tanınmayan her şey <see cref="Default"/> döner.
    /// </summary>
    public static string Normalize(string? candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return Default;
        }

        foreach (var supported in Supported)
        {
            if (string.Equals(candidate, supported, StringComparison.OrdinalIgnoreCase))
            {
                return supported;
            }
        }

        // Tam eşleşme yoksa dil ailesine göre karar verilir: en-GB, en-AU gibi
        // değerler en-US'e indirgenir, çünkü kullanıcı İngilizce istiyor ama
        // başka bir ülkenin katalogunu istemiyor.
        return TwoLetterOf(candidate) switch
        {
            "en" => English,
            "tr" => Turkish,
            _ => Default,
        };
    }

    /// <summary>
    /// Bir <see cref="CultureInfo"/>'yu izin verilen bir Market Locale'e eşler.
    /// </summary>
    public static string FromCulture(CultureInfo? culture) => Normalize(culture?.Name);

    private static string TwoLetterOf(string candidate)
    {
        var separatorIndex = candidate.IndexOf('-');
        var language = separatorIndex > 0 ? candidate[..separatorIndex] : candidate;

        return language.ToLowerInvariant();
    }
}
