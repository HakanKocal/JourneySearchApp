namespace Obilet.Application.Models;

/// <summary>
/// Bir Journey'in başlangıç veya bitiş noktası olabilen coğrafi yer.
/// obilet sisteminde tümü ilçe/semt granülerliğindedir.
/// </summary>
/// <param name="Id">API'nin Origin ve Destination olarak beklediği kimlik.</param>
/// <param name="Name">Kullanıcıya gösterilecek ad.</param>
/// <param name="Rank">
/// API'nin döndürdüğü görüntüleme sırası. Varsayılan Origin ve Destination
/// bu sıraya göre belirlenir; küçük değer daha popüler demektir.
/// </param>
/// <param name="Keywords">
/// API'nin metin aramasında eşleştirdiği ek anahtar kelimeler. Arama
/// sonuçlarının gerçekten ilgili olup olmadığını doğrulamak için gerekir.
/// </param>
public sealed record BusLocation(int Id, string Name, int? Rank, string? Keywords);
