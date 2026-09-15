using Obilet.Web.Formatting;

namespace Obilet.Tests.Formatting;

/// <summary>
/// Süre biçimlendirmesini doğrular.
/// </summary>
/// <remarks>
/// Bir kod incelemesinde bulunan hatayı koruyor: süre <c>h:mm</c> ile
/// biçimlendiriliyordu ve <c>h</c> saatin gün dışında kalan bileşenidir.
/// 25 saat 30 dakikalık bir sefer <c>1:30</c> olarak görünüyordu.
/// </remarks>
public sealed class DurationFormatterTests
{
    [Fact]
    public void Gunu_asan_sure_toplam_saat_olarak_gosterilir()
    {
        var duration = TimeSpan.FromMinutes(25 * 60 + 30);

        // Eski biçim burada "1:30" üretiyordu.
        Assert.Equal("25:30", DurationFormatter.Format(duration));
    }

    [Theory]
    [InlineData(7, 0, "7:00")]
    [InlineData(7, 25, "7:25")]
    [InlineData(0, 45, "0:45")]
    [InlineData(23, 59, "23:59")]
    [InlineData(24, 0, "24:00")]
    [InlineData(48, 15, "48:15")]
    public void Sure_saat_ve_dakika_olarak_bicimlendirilir(int hours, int minutes, string expected)
    {
        var duration = new TimeSpan(hours, minutes, 0);

        Assert.Equal(expected, DurationFormatter.Format(duration));
    }

    [Fact]
    public void Dakika_iki_haneli_yazilir()
    {
        Assert.Equal("6:05", DurationFormatter.Format(new TimeSpan(6, 5, 0)));
    }

    [Fact]
    public void Saniyeler_yuvarlanmaz_dusurulur()
    {
        // API saniye de gönderiyor; gösterimde dakika hassasiyeti yeterli.
        Assert.Equal("6:05", DurationFormatter.Format(new TimeSpan(6, 5, 59)));
    }

    [Fact]
    public void Sure_yoksa_null_doner()
    {
        Assert.Null(DurationFormatter.Format(null));
    }

    [Fact]
    public void Negatif_sure_gosterilmez()
    {
        Assert.Null(DurationFormatter.Format(TimeSpan.FromHours(-1)));
    }
}
