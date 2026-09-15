using Obilet.Application.Locations;

namespace Obilet.Tests.Locations;

/// <summary>
/// Türkçe'ye duyarlı arama karşılaştırmasını doğrular.
/// </summary>
/// <remarks>
/// Türkçe'de <c>i</c> harfinin iki büyük (<c>İ</c>, <c>I</c>) ve iki küçük
/// (<c>i</c>, <c>ı</c>) hâli olduğu için kültüre duyarlı küçültme tek başına
/// yetmiyor: <c>"Izmir".ToLower("tr-TR")</c> sonucu <c>ızmir</c> olur ve
/// <c>İzmir</c> ile eşleşmez. Bu testler noktasız I ile yazan kullanıcının
/// sonucu bulabildiğini koruyor.
/// </remarks>
public sealed class TurkishSearchTextTests
{
    [Theory]
    [InlineData("İzmir", "izmir")]
    [InlineData("Izmir", "izmir")]
    [InlineData("ızmir", "izmir")]
    [InlineData("IZMIR", "izmir")]
    [InlineData("İZMİR", "izmir")]
    public void i_harfinin_tum_varyantlari_ayni_degere_katlanir(string input, string expected)
    {
        Assert.Equal(expected, TurkishSearchText.Normalize(input));
    }

    [Theory]
    [InlineData("Şanlıurfa", "sanliurfa")]
    [InlineData("Kütahya", "kutahya")]
    [InlineData("Çorum", "corum")]
    [InlineData("Ğ", "g")]
    [InlineData("Nevşehir", "nevsehir")]
    public void Turkce_harfler_ascii_karsiliklarina_indirgenir(string input, string expected)
    {
        // Kullanıcılar Türkçe karakterleri sık sık ASCII yazıyor;
        // "sanliurfa" yazan biri "Şanlıurfa"yı bulmayı bekler.
        Assert.Equal(expected, TurkishSearchText.Normalize(input));
    }

    [Theory]
    [InlineData("Ankara (Aşti) Otogarı", "ankara asti otogari")]
    [InlineData("İstanbul   Avrupa", "istanbul avrupa")]
    [InlineData("  Ankara  ", "ankara")]
    [InlineData("Kayseri-Merkez", "kayseri merkez")]
    public void Noktalama_ve_fazla_bosluk_ayirici_sayilir(string input, string expected)
    {
        Assert.Equal(expected, TurkishSearchText.Normalize(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Bos_girdi_bos_dize_doner(string? input)
    {
        Assert.Equal(string.Empty, TurkishSearchText.Normalize(input));
    }

    [Theory]
    [InlineData("İzmir", "izmir")]
    [InlineData("İzmir", "Izmir")]
    [InlineData("İzmir", "IZM")]
    [InlineData("Şanlıurfa", "sanli")]
    [InlineData("Ankara (Aşti) Otogarı", "asti")]
    [InlineData("İstanbul Avrupa", "avrupa")]
    public void Iliskili_metinler_eslesir(string haystack, string needle)
    {
        Assert.True(TurkishSearchText.Contains(haystack, needle));
    }

    [Theory]
    [InlineData("İzmir", "ankara")]
    [InlineData("İzmir", "xqjz")]
    [InlineData("Ankara", "istanbul")]
    public void Iliskisiz_metinler_eslesmez(string haystack, string needle)
    {
        Assert.False(TurkishSearchText.Contains(haystack, needle));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Bos_arama_terimi_eslesme_saymaz(string? needle)
    {
        // Boş terim her şeyle eşleşirse süzme anlamsız hâle gelir.
        Assert.False(TurkishSearchText.Contains("İzmir", needle));
    }

    [Fact]
    public void Katlama_kullaniciya_gosterilen_metni_degistirmez()
    {
        // Normalleştirme yalnızca karşılaştırma içindir; bu test niyeti
        // belgeliyor — görünen ad her zaman API'nin verdiği hâliyle kalır.
        const string original = "İstanbul Avrupa";

        Assert.Equal("istanbul avrupa", TurkishSearchText.Normalize(original));
        Assert.Equal("İstanbul Avrupa", original);
    }
}
