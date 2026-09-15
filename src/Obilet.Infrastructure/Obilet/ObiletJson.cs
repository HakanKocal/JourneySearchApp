using System.Text.Json;
using System.Text.Json.Serialization;

namespace Obilet.Infrastructure.Obilet;

/// <summary>
/// obilet API'si ile konuşurken kullanılan serileştirme ayarları.
/// </summary>
/// <remarks>
/// Tek bir yerde tanımlıdır, çünkü API'nin sözleşmesi baştan sona kebab-case
/// kullanıyor (<c>session-id</c>, <c>device-session</c>, <c>origin-id</c>) ve
/// bunu her istek ve yanıt tipinde tek tek belirtmek hataya açık olurdu.
/// </remarks>
internal static class ObiletJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.KebabCaseLower,
        PropertyNameCaseInsensitive = true,

        // API, göndermediğimiz alanları varsayılan değerleriyle ele alıyor;
        // null alanları göndermemek istek gövdelerini gereksiz büyütmüyor.
        // Ancak Bus Location aramasında "data": null anlamlı bir değerdir
        // (tüm liste demektir), bu yüzden null'lar yazılmaya devam eder.
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };
}
