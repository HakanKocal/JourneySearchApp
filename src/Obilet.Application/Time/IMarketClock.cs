namespace Obilet.Application.Time;

/// <summary>
/// Pazarın yerel saatini sağlar.
/// </summary>
/// <remarks>
/// <para>
/// Bu soyutlamanın var olma sebebi somut bir hata: tarih hesapları
/// <c>TimeProvider.GetLocalNow()</c> ile yapılıyordu, yani <b>sunucunun</b>
/// saat dilimine göre. Konteyner imajı UTC çalışıyor, pazar ise Türkiye
/// (UTC+3). Ölçülen fark: konteynerde <c>20:24 UTC</c> iken host'ta
/// <c>23:24</c>.
/// </para>
/// <para>
/// Sonucu ciddiydi: her gece 21:00–00:00 arasında sunucunun "bugün"ü
/// kullanıcının dünü oluyordu. O pencerede <c>Yarın</c> varsayılanı bugünü
/// gösteriyor, <c>Bugün</c> çipi dünü arıyor ve şartnamenin "minimum geçerli
/// tarih bugündür" kuralı doğrulamadan geçiyordu
/// (<c>dün &lt; dün</c> yanlış olduğu için).
/// </para>
/// <para>
/// Bu yüzden "bugün" artık host'un saat diliminden değil, pazarın saat
/// diliminden okunuyor. Uygulama nerede çalışırsa çalışsın sonuç aynı.
/// </para>
/// </remarks>
public interface IMarketClock
{
    /// <summary>Pazarın yerel saatine göre bugünün tarihi.</summary>
    DateOnly Today { get; }

    /// <summary>Pazarın yerel saatine göre şu an.</summary>
    DateTimeOffset Now { get; }
}
