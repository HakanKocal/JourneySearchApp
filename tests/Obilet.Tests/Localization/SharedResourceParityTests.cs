using System.Collections;
using System.Globalization;
using System.Resources;
using Obilet.Application.Localization;
using Obilet.Web;

namespace Obilet.Tests.Localization;

/// <summary>
/// Kaynak dosyalarının eksiksiz olduğunu doğrular.
/// </summary>
/// <remarks>
/// Eksik bir çeviri anahtarı çalışma zamanında hata vermez: yerelleştirme
/// altyapısı sessizce anahtarın kendisini ekrana basar. Yani
/// <c>Search_Submit</c> gibi bir metin kullanıcıya görünür ve bu, gözden
/// kaçması en kolay kusur türüdür. Bu test onu derleme sonrası yakalar.
/// </remarks>
public sealed class SharedResourceParityTests
{
    private static readonly ResourceManager Resources = new(
        $"{typeof(SharedResource).Namespace}.Resources.{nameof(SharedResource)}",
        typeof(SharedResource).Assembly);

    /// <summary>
    /// Verilen dilin tüm anahtar/değer çiftlerini döndürür.
    /// </summary>
    /// <remarks>
    /// Kaynak kümesi bilinçli olarak <c>Dispose</c> edilmiyor:
    /// <see cref="ResourceManager"/> kümeleri önbelleğinde tutuyor ve
    /// kapatılan bir küme sonraki erişimlerde
    /// <see cref="ObjectDisposedException"/> fırlatıyor. Kümelerin sahibi
    /// manager'ın kendisi.
    /// </remarks>
    private static Dictionary<string, string?> EntriesFor(string cultureName)
    {
        var culture = new CultureInfo(cultureName);

        var set = Resources.GetResourceSet(culture, createIfNotExists: true, tryParents: true)
            ?? throw new InvalidOperationException($"'{cultureName}' için kaynak kümesi bulunamadı.");

        var entries = new Dictionary<string, string?>(StringComparer.Ordinal);

        foreach (DictionaryEntry entry in set)
        {
            entries[(string)entry.Key] = entry.Value as string;
        }

        return entries;
    }

    [Fact]
    public void Turkce_kaynak_dosyasi_metin_iceriyor()
    {
        var entries = EntriesFor(MarketLocale.Turkish);

        // Boş bir kaynak kümesi, diğer testlerin sessizce geçmesine yol açardı.
        Assert.NotEmpty(entries);
        Assert.Equal("Bileti Bul", entries["Search_Submit"]);
    }

    [Fact]
    public void Desteklenen_her_dil_ayni_anahtarlari_icerir()
    {
        var turkish = EntriesFor(MarketLocale.Turkish).Keys.ToHashSet(StringComparer.Ordinal);
        var english = EntriesFor(MarketLocale.English).Keys.ToHashSet(StringComparer.Ordinal);

        var missingInEnglish = turkish.Except(english, StringComparer.Ordinal).ToList();
        var missingInTurkish = english.Except(turkish, StringComparer.Ordinal).ToList();

        Assert.True(
            missingInEnglish.Count == 0,
            $"İngilizce kaynak dosyasında eksik anahtarlar: {string.Join(", ", missingInEnglish)}");

        Assert.True(
            missingInTurkish.Count == 0,
            $"Türkçe kaynak dosyasında eksik anahtarlar: {string.Join(", ", missingInTurkish)}");
    }

    [Fact]
    public void Hicbir_ceviri_bos_degil()
    {
        foreach (var cultureName in MarketLocale.Supported)
        {
            foreach (var (key, value) in EntriesFor(cultureName))
            {
                Assert.False(
                    string.IsNullOrWhiteSpace(value),
                    $"'{cultureName}' dilinde '{key}' anahtarının değeri boş.");
            }
        }
    }

    [Fact]
    public void Ceviriler_gercekten_farkli_dillerde()
    {
        var english = EntriesFor(MarketLocale.English);

        // Yarım bırakılmış bir çeviri dosyası, anahtarları Türkçe değerlerle
        // kopyalanmış hâlde bırakır ve parite testi yine geçer. Bilinen
        // birkaç metnin gerçekten çevrildiğini ayrıca kontrol ediyoruz.
        var expectations = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Search_Origin"] = "From",
            ["Search_Destination"] = "To",
            ["Search_Today"] = "Today",
            ["Journey_Back"] = "Back",
        };

        foreach (var (key, expected) in expectations)
        {
            Assert.Equal(expected, english[key]);
        }
    }

    [Fact]
    public void Dil_adlari_cevrilmez()
    {
        // Dil adları her zaman kendi dilinde yazılır: İngilizce arayüzde de
        // "Türkçe" görünmeli, "Turkish" değil. Aksi hâlde kullanıcı kendi
        // dilini tanıyamaz.
        foreach (var cultureName in MarketLocale.Supported)
        {
            var entries = EntriesFor(cultureName);

            Assert.Equal("Türkçe", entries["Culture_Turkish"]);
            Assert.Equal("English", entries["Culture_English"]);
        }
    }

    [Fact]
    public void Bicimlendirme_yer_tutuculari_diller_arasinda_ayni()
    {
        var turkish = EntriesFor(MarketLocale.Turkish);
        var english = EntriesFor(MarketLocale.English);

        // {0} gibi yer tutucuların sayısı dillere göre değişirse
        // string.Format çalışma zamanında hata verir. Çeviri sırasında
        // kolayca düşürülen bir ayrıntı.
        foreach (var (key, turkishValue) in turkish)
        {
            var expected = CountPlaceholders(turkishValue);
            var actual = CountPlaceholders(english[key]);

            Assert.True(
                expected == actual,
                $"'{key}' anahtarında yer tutucu sayısı uyuşmuyor: tr={expected}, en={actual}.");
        }
    }

    private static int CountPlaceholders(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return 0;
        }

        var count = 0;

        for (var index = 0; index < 10; index++)
        {
            if (value.Contains($"{{{index}}}", StringComparison.Ordinal))
            {
                count++;
            }
        }

        return count;
    }
}
