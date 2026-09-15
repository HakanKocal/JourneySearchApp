using System.ComponentModel.DataAnnotations;

namespace Obilet.Infrastructure.Obilet;

/// <summary>
/// obilet business API'sine bağlanmak için gereken yapılandırma.
/// </summary>
public sealed class ObiletApiOptions
{
    /// <summary>Yapılandırma dosyasındaki bölüm adı.</summary>
    public const string SectionName = "ObiletApi";

    /// <summary>API'nin kök adresi.</summary>
    [Required]
    public string BaseUrl { get; init; } = "https://v2-api.obilet.com/api/";

    /// <summary>
    /// Uygulamanın kendisini API'ye tanıttığı sabit kimlik. Ziyaretçiye özel
    /// değildir; <c>Authorization: Basic</c> başlığında gönderilir.
    /// </summary>
    /// <remarks>
    /// Bu değer ödev dokümanında açıkça verildiği için yapılandırma dosyasında
    /// tutulur ve projenin klonlandığı anda ek kurulum gerektirmeden çalışır.
    /// Gerçek bir üretim ortamında ortam değişkeni veya secret deposu kullanılır.
    /// </remarks>
    [Required]
    public string ApiClientToken { get; init; } = string.Empty;

    /// <summary>
    /// Tek bir API isteği için beklenecek azami süre.
    /// </summary>
    /// <remarks>
    /// Açıkça ayarlanması zorunludur. API, tanımadığı bir Market Locale
    /// değerinde hata döndürmek yerine isteği süresiz askıda bırakıyor;
    /// <see cref="HttpClient"/>'ın 100 saniyelik varsayılanı bu davranışla
    /// birleştiğinde bekleyen istek yığılmasına yol açar. Bkz. docs/adr/0002.
    /// </remarks>
    [Range(1, 120)]
    public int TimeoutSeconds { get; init; } = 15;

    /// <summary>
    /// Oturum oluştururken API'ye bildirilen sunucu IP adresi.
    /// </summary>
    public string OutboundIpAddress { get; init; } = "165.114.41.21";

    /// <summary>Oturum oluştururken API'ye bildirilen port.</summary>
    public string OutboundPort { get; init; } = "5117";

    /// <summary>
    /// API'nin beklediği istemci tipi.
    /// </summary>
    /// <remarks>
    /// Resmî doküman bu alan için <c>7</c> değerini ve yanında bir
    /// <c>application</c> nesnesi belgeliyor, ancak API bu gövdeyi reddediyor:
    /// tarayıcı tipli istemciler için <c>port</c> ve <c>browser</c> alanlarını
    /// zorunlu tutuyor. Çalışan gövde, örnek Postman koleksiyonundaki
    /// <c>1</c> değerli tarayıcı tipidir.
    /// </remarks>
    public int DeviceType { get; init; } = 1;

    /// <summary>Oturum oluştururken bildirilen tarayıcı adı.</summary>
    public string BrowserName { get; init; } = "Chrome";

    /// <summary>Oturum oluştururken bildirilen tarayıcı sürümü.</summary>
    public string BrowserVersion { get; init; } = "47.0.0.12";
}
