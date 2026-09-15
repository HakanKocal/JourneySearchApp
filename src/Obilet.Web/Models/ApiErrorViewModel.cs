namespace Obilet.Web.Models;

/// <summary>
/// API hatasının kullanıcıya nasıl anlatılacağını belirler.
/// </summary>
public enum ApiErrorKind
{
    /// <summary>Beklenmeyen bir arıza.</summary>
    Unexpected,

    /// <summary>Hız sınırı: şu an çok fazla istek var.</summary>
    RateLimited,
}

/// <summary>
/// API hatası sayfasının view model'i.
/// </summary>
/// <remarks>
/// Upstream hata mesajı bilinçli olarak <b>taşınmaz</b>. API başarısızlıkta
/// kendi sunucu tarafı yığın izini döndürüyor ve bunun kullanıcıya ulaşması
/// kabul edilemez. Kullanıcıya yalnızca ne olduğu ve destek için
/// kullanılabilecek bir izleme kimliği gösterilir.
/// </remarks>
/// <param name="Kind">Hatanın kullanıcıya anlatılacak türü.</param>
/// <param name="ReferenceId">
/// İsteği loglarda bulmaya yarayan kimlik. Kullanıcı bunu destekle
/// paylaşabilir; hiçbir hassas bilgi içermez.
/// </param>
public sealed record ApiErrorViewModel(ApiErrorKind Kind, string? ReferenceId);
