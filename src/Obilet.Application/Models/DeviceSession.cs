namespace Obilet.Application.Models;

/// <summary>
/// obilet API'sinin, adına çağrı yapılabilmesi için talep ettiği kimlik çifti.
/// Uygulamanın son kullanıcı adına API ile konuşma yetkisini temsil eder.
/// </summary>
/// <remarks>
/// Bu bir kimlik bilgisidir. Hiçbir view model'e, JSON yanıtına veya cookie'ye
/// konulmamalı; yalnızca sunucu tarafında, Visitor Session içinde yaşar.
/// Terim ayrımı için bkz. CONTEXT.md.
/// </remarks>
public sealed record DeviceSession(string SessionId, string DeviceId)
{
    /// <summary>
    /// Çiftin API'ye gönderilebilir durumda olup olmadığını söyler. Boş bir
    /// oturum, hiç oluşturulmamış oturumla aynı şekilde ele alınır.
    /// </summary>
    public bool IsUsable =>
        !string.IsNullOrWhiteSpace(SessionId) && !string.IsNullOrWhiteSpace(DeviceId);
}
