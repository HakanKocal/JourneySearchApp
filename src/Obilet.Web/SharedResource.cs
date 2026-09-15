namespace Obilet.Web;

/// <summary>
/// Paylaşılan arayüz metinlerinin kaynak dosyasını işaretleyen tip.
/// </summary>
/// <remarks>
/// <para>
/// Üyesi yoktur; yalnızca <c>IStringLocalizer&lt;SharedResource&gt;</c>
/// çağrılarının hangi <c>.resx</c> dosyasını kullanacağını belirtmeye yarar.
/// Metinleri controller ve view başına ayrı dosyalara bölmek yerine tek bir
/// paylaşılan dosyada tutuyoruz: bu boyutta bir uygulamada bölme, bulmayı
/// kolaylaştırmaktan çok zorlaştırırdı.
/// </para>
/// <para>
/// Anahtarlar bilinçli olarak nötr (<c>Locations_Title</c> gibi), Türkçe
/// metnin kendisi anahtar olarak kullanılmaz. Aksi hâlde Türkçe bir ifadeyi
/// düzeltmek, tüm çeviri dosyalarındaki anahtarı da kırardı.
/// </para>
/// </remarks>
public sealed class SharedResource;
