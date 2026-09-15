using System.Globalization;
using Obilet.Web.Formatting;

namespace Obilet.Tests.Formatting;

/// <summary>
/// Para biçimlendirmesini doğrular.
/// </summary>
/// <remarks>
/// Bu testler somut bir hatayı koruyor: <c>ToString("C2")</c> ambient
/// kültürün para birimi sembolünü kullandığı için, arayüz İngilizceye
/// geçtiğinde Türk Lirası cinsinden tutarlar dolar olarak görünüyordu.
/// </remarks>
public sealed class MoneyFormatterTests
{
    /// <summary>Verilen kültür altında bir eylemi çalıştırır.</summary>
    private static T InCulture<T>(string cultureName, Func<T> action)
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            var culture = new CultureInfo(cultureName);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            return action();
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Fact]
    public void Turkce_kulturde_virgullu_ondalik_ve_lira_sembolu_kullanilir()
    {
        var result = InCulture("tr-TR", () => MoneyFormatter.Format(900m, "TRY"));

        Assert.Equal("900,00 ₺", result);
    }

    [Fact]
    public void Ingilizce_kulturde_sayi_bicimi_degisir_ama_para_birimi_degismez()
    {
        var result = InCulture("en-US", () => MoneyFormatter.Format(900m, "TRY"));

        // Asıl korunan davranış: sayı biçimi kültüre uyar, para birimi uymaz.
        Assert.Equal("900.00 ₺", result);
        Assert.DoesNotContain("$", result);
    }

    [Fact]
    public void Binlik_gruplama_kulture_gore_yapilir()
    {
        Assert.Equal("1.499,50 ₺", InCulture("tr-TR", () => MoneyFormatter.Format(1499.5m, "TRY")));
        Assert.Equal("1,499.50 ₺", InCulture("en-US", () => MoneyFormatter.Format(1499.5m, "TRY")));
    }

    [Theory]
    [InlineData("EUR", "€")]
    [InlineData("USD", "$")]
    [InlineData("GBP", "£")]
    public void Bilinen_para_birimleri_sembole_cevrilir(string code, string symbol)
    {
        var result = InCulture("tr-TR", () => MoneyFormatter.Format(100m, code));

        Assert.EndsWith(symbol, result);
    }

    [Fact]
    public void Bilinmeyen_para_birimi_kodun_kendisiyle_gosterilir()
    {
        var result = InCulture("tr-TR", () => MoneyFormatter.Format(100m, "XYZ"));

        // Tanımadığımız bir para birimini yanlış bir sembolle göstermektense
        // ISO kodunu göstermek doğrudur.
        Assert.Equal("100,00 XYZ", result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Para_birimi_yoksa_yalnizca_tutar_gosterilir(string? code)
    {
        var result = InCulture("tr-TR", () => MoneyFormatter.Format(100m, code));

        Assert.Equal("100,00", result);
    }

    [Fact]
    public void Kurus_basamaklari_korunur()
    {
        Assert.Equal("499,90 ₺", InCulture("tr-TR", () => MoneyFormatter.Format(499.9m, "TRY")));
        Assert.Equal("0,05 ₺", InCulture("tr-TR", () => MoneyFormatter.Format(0.05m, "TRY")));
    }
}
