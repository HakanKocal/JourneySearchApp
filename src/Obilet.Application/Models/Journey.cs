namespace Obilet.Application.Models;

/// <summary>
/// Belirli bir Partner tarafından, belirli bir Origin'den belirli bir
/// Destination'a, belirli bir kalkış anında işletilen tek bir otobüs seferi.
/// </summary>
/// <remarks>
/// <para>
/// Bu model bilinçli olarak incedir. API sefer başına yüzden fazla alan
/// döndürüyor ve popüler bir hat için yanıt birkaç megabayta ulaşıyor;
/// arayüzün ihtiyaç duyduğu alanlara indirmek hem belleği hem de serileştirme
/// maliyetini büyük ölçüde düşürüyor.
/// </para>
/// <para>
/// Ayrıca bir sınır görevi görür: arayüz katmanı API'nin sözleşmesine değil,
/// bu modele bağımlıdır. API yeni alanlar eklediğinde veya adlarını
/// değiştirdiğinde etkilenen tek yer Infrastructure katmanıdır.
/// </para>
/// </remarks>
/// <param name="Id">Seferin kimliği.</param>
/// <param name="PartnerId">Seferi işleten firmanın kimliği. Logo adresinde kullanılır.</param>
/// <param name="PartnerName">Seferi işleten firmanın adı.</param>
/// <param name="BusType">Koltuk düzeni (örneğin <c>2+1</c>).</param>
/// <param name="TotalSeats">Otobüsteki toplam koltuk sayısı.</param>
/// <param name="AvailableSeats">Boş koltuk sayısı.</param>
/// <param name="OriginStation">Kalkış terminalinin adı.</param>
/// <param name="DestinationStation">Varış terminalinin adı.</param>
/// <param name="OriginLocation">
/// Kalkış lokasyonunun (şehir) adı — terminal adı değil.
/// </param>
/// <param name="DestinationLocation">
/// Varış lokasyonunun (şehir) adı — terminal adı değil.
/// </param>
/// <param name="Departure">
/// Kalkış anı. Tarih bileşeni önemlidir: dönen küme istenen günle sınırlı
/// değil, gece yarısını aşan seferler ertesi güne taşıyor.
/// </param>
/// <param name="Arrival">Varış anı.</param>
/// <param name="Duration">Yolculuk süresi.</param>
/// <param name="OriginalPrice">İndirim öncesi liste fiyatı.</param>
/// <param name="InternetPrice">Çevrimiçi satış fiyatı; kullanıcının ödeyeceği tutar.</param>
/// <param name="Currency">Fiyatların para birimi (ISO 4217).</param>
/// <param name="Features">
/// Seferin Feature'ları, API'nin öncelik sırasına göre sıralanmış hâlde.
/// </param>
/// <remarks>
/// Feature listesi bilinçli olarak <b>sınırlanmıyor</b>. Bir süre kartta en
/// çok dört öğe gösteriliyordu; gerekçe, hepsini taşımanın ince
/// projeksiyondan kazanılan boyutu geri alacağıydı. Ölçüm bunu yanlışladı:
/// 462 seferlik bir listede kapağı kaldırmak ham HTML'i 2.161.286 bayttan
/// 2.226.664 bayta, sıkıştırılmış hâlini 40.160 bayttan 40.730 bayta
/// çıkarıyor — yani yanıtın <b>%1,4'ü</b>, ağ üzerinde 570 bayt. Asıl
/// kazanç API yanıtını bu ince modele indirmekten geliyordu, kapaktan değil.
/// Karşılığında kapak gerçek bir tutarsızlık üretiyordu: tanıtım etiketi de
/// aynı dört slotu paylaştığı için indirim kodu olan bir sefer komşusundan
/// bir ikon az gösteriyordu.
/// </remarks>
public sealed record Journey(
    long Id,
    int PartnerId,
    string PartnerName,
    string? BusType,
    int TotalSeats,
    int AvailableSeats,
    string? OriginStation,
    string? DestinationStation,
    string? OriginLocation,
    string? DestinationLocation,
    DateTime Departure,
    DateTime Arrival,
    TimeSpan? Duration,
    decimal OriginalPrice,
    decimal InternetPrice,
    string? Currency,
    IReadOnlyList<Feature> Features)
{
    /// <summary>
    /// Liste fiyatının satış fiyatından yüksek olup olmadığını söyler.
    /// </summary>
    /// <remarks>
    /// Yalnızca gerçek bir indirim varsa üstü çizili fiyat gösterilmeli;
    /// iki fiyat eşitken çizili fiyat göstermek kullanıcıyı yanıltır.
    /// </remarks>
    public bool HasDiscount => OriginalPrice > InternetPrice;
}
