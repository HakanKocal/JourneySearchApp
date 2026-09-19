namespace Obilet.Application.Models;

/// <summary>
/// Bir Journey'de sunulan olanak; örneğin kablosuz internet veya priz.
/// </summary>
/// <remarks>
/// <para>
/// Tanıtımlı olanlar (<see cref="IsPromoted"/>) olanak değil ticari bilgi
/// taşır: canlı veride çoğu indirim kodu ("150₺ İndirim Kodu"), kalanı
/// sefer niteliği ("Jumbo Sefer", "Yeni Otoban"). API bunlara özel gösterim
/// renkleri de bildirdiği için arayüzde ikon değil metin etiketi olarak
/// sunulurlar — bir indirim tutarı sembole sığmaz.
/// </para>
/// <para>
/// Ad, API'nin çevrilen alanından gelir. Aynı bilgi yanıtta bir de düz metin
/// dizisi olarak bulunuyor ama o dizi İngilizce istekte de Türkçe kalıyor;
/// yanlış alanı seçmek hata vermez, yalnızca İngilizce sayfada Türkçe metin
/// gösterir.
/// </para>
/// </remarks>
/// <param name="Id">İkon adresini çözmek için kullanılan kimlik.</param>
/// <param name="Name">Kullanıcıya gösterilecek ad.</param>
/// <param name="IsPromoted">Tanıtımlı bir özellik mi.</param>
/// <param name="BackgroundColor">
/// Tanıtımlı özelliğin zemin rengi. API'den gelir, doğrulanmadan
/// kullanılmamalıdır.
/// </param>
/// <param name="ForegroundColor">Tanıtımlı özelliğin metin rengi.</param>
public sealed record Feature(
    int Id,
    string Name,
    bool IsPromoted,
    string? BackgroundColor,
    string? ForegroundColor);
