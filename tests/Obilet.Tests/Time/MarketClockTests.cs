using Microsoft.Extensions.Time.Testing;
using Obilet.Application.Time;

namespace Obilet.Tests.Time;

/// <summary>
/// Pazarın saatinin sunucunun saat diliminden bağımsız olduğunu doğrular.
/// </summary>
/// <remarks>
/// Bu testler bir kod incelemesinde bulunan hatayı koruyor: tarih hesapları
/// <c>GetLocalNow()</c> ile yapılıyordu, yani sunucunun saat dilimine göre.
/// Konteyner imajı UTC çalışıyor, pazar ise UTC+3. Ölçülen fark 3 saatti ve
/// her gece 21:00–00:00 arasında sunucunun "bugün"ü kullanıcının dünü
/// oluyordu; o pencerede şartnamenin "minimum geçerli tarih bugündür"
/// kuralı doğrulamadan geçiyordu.
/// </remarks>
public sealed class MarketClockTests
{
    private static MarketClock CreateSut(DateTimeOffset utcNow)
    {
        var timeProvider = new FakeTimeProvider(utcNow);

        // Sunucunun saat dilimi bilinçli olarak UTC bırakılıyor: konteynerde
        // durum bu ve testin anlamı tam olarak buradan geliyor.
        timeProvider.SetLocalTimeZone(TimeZoneInfo.Utc);

        return new MarketClock(timeProvider);
    }

    [Fact]
    public void Gece_yarisindan_once_UTC_iken_pazarda_ertesi_gun_baslamistir()
    {
        // 16 Eylül 22:30 UTC = 17 Eylül 01:30 Türkiye.
        var sut = CreateSut(new DateTimeOffset(2026, 9, 16, 22, 30, 0, TimeSpan.Zero));

        // Sunucunun yerel saatine göre bugün 16 Eylül olurdu; pazar saatine
        // göre 17 Eylül. Kullanıcının gördüğü tarih ikincisi.
        Assert.Equal(new DateOnly(2026, 9, 17), sut.Today);
    }

    [Fact]
    public void Gun_icinde_pazar_ve_UTC_ayni_gunu_gosterir()
    {
        var sut = CreateSut(new DateTimeOffset(2026, 9, 16, 9, 0, 0, TimeSpan.Zero));

        Assert.Equal(new DateOnly(2026, 9, 16), sut.Today);
    }

    [Fact]
    public void Pazar_saati_UTC_den_uc_saat_ileridedir()
    {
        var sut = CreateSut(new DateTimeOffset(2026, 9, 16, 12, 0, 0, TimeSpan.Zero));

        // Türkiye 2016'dan beri yaz saati uygulamıyor; fark yıl boyunca +3.
        Assert.Equal(TimeSpan.FromHours(3), sut.Now.Offset);
        Assert.Equal(15, sut.Now.Hour);
    }

    [Fact]
    public void Kis_ayinda_da_fark_uc_saattir()
    {
        var sut = CreateSut(new DateTimeOffset(2027, 1, 15, 12, 0, 0, TimeSpan.Zero));

        Assert.Equal(TimeSpan.FromHours(3), sut.Now.Offset);
    }

    [Fact]
    public void Gun_sinirindaki_esik_dogru()
    {
        // 20:59:59 UTC → pazarda 23:59:59, hâlâ aynı gün.
        var oncesi = CreateSut(new DateTimeOffset(2026, 9, 16, 20, 59, 59, TimeSpan.Zero));
        Assert.Equal(new DateOnly(2026, 9, 16), oncesi.Today);

        // 21:00:00 UTC → pazarda 00:00:00, yeni gün başlar.
        var sonrasi = CreateSut(new DateTimeOffset(2026, 9, 16, 21, 0, 0, TimeSpan.Zero));
        Assert.Equal(new DateOnly(2026, 9, 17), sonrasi.Today);
    }

    [Fact]
    public void Dogrulama_pazarin_gunune_gore_calisir()
    {
        // Hatanın asıl sonucu buydu: sunucu 16 Eylül derken kullanıcı
        // 17 Eylül'deydi ve 16 Eylül geçmiş bir tarih olduğu hâlde
        // doğrulamadan geçiyordu.
        var sut = CreateSut(new DateTimeOffset(2026, 9, 16, 22, 30, 0, TimeSpan.Zero));

        var gecmis = new DateOnly(2026, 9, 16);

        Assert.False(
            Application.Journeys.SearchQueryValidator.IsValid(349, 356, gecmis, sut.Today));
    }
}
