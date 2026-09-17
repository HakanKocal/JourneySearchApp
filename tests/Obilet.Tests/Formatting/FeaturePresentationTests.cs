using Obilet.Web.Formatting;

namespace Obilet.Tests.Formatting;

/// <summary>
/// Olanak ikonu adresini ve renk doğrulamasını sınar.
/// </summary>
public sealed class FeaturePresentationTests
{
    [Theory]
    [InlineData(26, "https://s3.eu-central-1.amazonaws.com/static.obilet.com/images/feature/26.svg")]
    [InlineData(10, "https://s3.eu-central-1.amazonaws.com/static.obilet.com/images/feature/10.svg")]
    [InlineData(7, "https://s3.eu-central-1.amazonaws.com/static.obilet.com/images/feature/7.svg")]
    public void Ikon_adresi_dokumandaki_kalibi_izler(int featureId, string expected)
    {
        // Kalıp API dokümanında entegratörler için belgelenmiş; canlı olarak
        // bu kimliklerin 200 image/svg+xml döndürdüğü doğrulandı.
        Assert.Equal(expected, FeatureIcon.UrlFor(featureId));
    }

    [Theory]
    [InlineData("#dffadc")]
    [InlineData("#DADBDD")]
    [InlineData("#7C8A9F")]
    [InlineData("#fff")]
    [InlineData("#FFFFFFAA")]
    public void Gecerli_renk_sabitleri_kabul_edilir(string value)
    {
        // Canlı veride gözlenen değerler bu biçimlerde.
        Assert.Equal(value, CssColor.Sanitize(value));
    }

    [Fact]
    public void Bosluklar_kirpilir()
    {
        Assert.Equal("#dffadc", CssColor.Sanitize("  #dffadc  "));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("red")]
    [InlineData("rgb(255,0,0)")]
    [InlineData("dffadc")]
    [InlineData("#dffad")]
    [InlineData("#dffadcx")]
    [InlineData("#zzzzzz")]
    public void Tanimayan_degerler_reddedilir(string? value)
    {
        // Renk gösterilememesi kabul edilebilir; güvenilmeyen metni
        // işaretlemeye yazmak değil.
        Assert.Null(CssColor.Sanitize(value));
    }

    [Theory]
    [InlineData("#fff;background-image:url(javascript:alert(1))")]
    [InlineData("#fff\" onload=\"alert(1)")]
    [InlineData("#fff;}body{display:none")]
    [InlineData("red;color:blue")]
    [InlineData("expression(alert(1))")]
    public void Enjeksiyon_denemeleri_reddedilir(string value)
    {
        // Renkler obilet API'sinden geliyor ve doğrudan bir style
        // özniteliğine yazılıyor; doğrulanmamış bir değer burada bir
        // enjeksiyon yolu olurdu.
        Assert.Null(CssColor.Sanitize(value));
    }
}
