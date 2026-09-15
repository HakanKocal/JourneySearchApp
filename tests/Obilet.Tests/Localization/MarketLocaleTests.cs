using System.Globalization;
using Obilet.Application.Localization;

namespace Obilet.Tests.Localization;

/// <summary>
/// Display Culture değerinin API'ye gönderilecek Market Locale'e nasıl
/// indirgendiğini doğrular.
/// </summary>
/// <remarks>
/// Bu testler canlı API'de doğrulanmış iki davranışı koruyor. Birincisi,
/// <c>language</c> alanı bir görüntü dili değil pazar seçicidir: <c>en-GB</c>
/// göndermek kullanıcıyı Britanya otobüs hatlarına düşürür. İkincisi,
/// tanınmayan değerler — resmî dokümanın varsayılan olarak belgelediği
/// <c>en-EN</c> dahil — isteği süresiz askıda bırakır.
/// </remarks>
public sealed class MarketLocaleTests
{
    [Theory]
    [InlineData("tr-TR", MarketLocale.Turkish)]
    [InlineData("en-US", MarketLocale.English)]
    public void Desteklenen_degerler_oldugu_gibi_kalir(string input, string expected)
    {
        Assert.Equal(expected, MarketLocale.Normalize(input));
    }

    [Theory]
    [InlineData("TR-tr", MarketLocale.Turkish)]
    [InlineData("EN-us", MarketLocale.English)]
    public void Buyuk_kucuk_harf_farki_onemsizdir(string input, string expected)
    {
        Assert.Equal(expected, MarketLocale.Normalize(input));
    }

    [Theory]
    [InlineData("en-GB")]
    [InlineData("en-AU")]
    [InlineData("en")]
    public void Diger_ingilizce_varyantlari_en_US_e_indirgenir(string input)
    {
        // en-GB gerçek bir pazardır ve Britanya lokasyonlarını döndürür.
        // Kullanıcı İngilizce istiyor, başka bir ülkenin katalogunu istemiyor.
        Assert.Equal(MarketLocale.English, MarketLocale.Normalize(input));
    }

    [Theory]
    [InlineData("de-DE")]
    [InlineData("fr-FR")]
    [InlineData("ru-RU")]
    [InlineData("ar-SA")]
    [InlineData("xx-XX")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Desteklenmeyen_degerler_varsayilana_duser(string? input)
    {
        Assert.Equal(MarketLocale.Default, MarketLocale.Normalize(input));
    }

    [Fact]
    public void Dokumandaki_bozuk_deger_asla_uretilmez()
    {
        // Resmî doküman en-EN değerini varsayılan olarak belgeliyor, ancak bu
        // değer isteği süresiz askıda bırakıyor. Hiçbir girdi bu değeri
        // üretmemeli. Bu test, birinin "doküman böyle diyor" diyerek geri
        // dönmesini engellemek için var.
        string?[] everyKindOfInput =
        [
            null, "", "   ", "en-EN", "EN-en", "en", "en-US", "en-GB",
            "tr", "tr-TR", "de-DE", "xx-XX", "invalid",
        ];

        foreach (var input in everyKindOfInput)
        {
            var result = MarketLocale.Normalize(input);

            Assert.NotEqual(MarketLocale.DocumentedButBroken, result);
            Assert.Contains(result, MarketLocale.Supported);
        }
    }

    [Fact]
    public void Dokumandaki_bozuk_deger_dogrudan_verilse_bile_reddedilir()
    {
        Assert.Equal(
            MarketLocale.English,
            MarketLocale.Normalize(MarketLocale.DocumentedButBroken));
    }

    [Fact]
    public void Kultur_nesnesinden_esleme_yapilir()
    {
        Assert.Equal(
            MarketLocale.English,
            MarketLocale.FromCulture(new CultureInfo("en-US")));

        Assert.Equal(
            MarketLocale.Turkish,
            MarketLocale.FromCulture(new CultureInfo("tr-TR")));
    }

    [Fact]
    public void Kultur_yoksa_varsayilan_kullanilir()
    {
        Assert.Equal(MarketLocale.Default, MarketLocale.FromCulture(null));
    }

    [Fact]
    public void Cozumleyici_ambient_kulturu_kullanir()
    {
        var original = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
            Assert.Equal(MarketLocale.English, new MarketLocaleResolver().Resolve());

            CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
            Assert.Equal(MarketLocale.Turkish, new MarketLocaleResolver().Resolve());
        }
        finally
        {
            CultureInfo.CurrentUICulture = original;
        }
    }

    [Fact]
    public void Desteklenen_liste_sadece_dogrulanmis_degerleri_icerir()
    {
        // Listeye yeni bir değer eklemek, gerçek bir API çağrısıyla
        // doğrulanmasını gerektirir: her locale farklı bir ülke katalogu
        // döndürüyor veya hiç dönmüyor. Bu test, listenin sessizce
        // büyümesini fark edilir kılar.
        Assert.Equal(["tr-TR", "en-US"], MarketLocale.Supported);
    }
}
