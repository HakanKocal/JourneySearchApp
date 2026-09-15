namespace Obilet.Application.Abstractions;

/// <summary>
/// Tek bir ziyaretçiye ait sunucu taraflı durumun deposu.
/// </summary>
/// <remarks>
/// Uygulama katmanının ASP.NET Core'un oturum tiplerine bağımlı olmaması için
/// soyutlanmıştır; somut uygulaması web katmanındadır. Böylece Device Session
/// orkestrasyonu HTTP bağlamı olmadan test edilebilir.
/// </remarks>
public interface IVisitorSessionStore
{
    /// <summary>Anahtara karşılık gelen değeri döndürür, yoksa <c>null</c>.</summary>
    string? Get(string key);

    /// <summary>Anahtara değer yazar.</summary>
    void Set(string key, string value);

    /// <summary>Anahtarı siler. Yoksa hiçbir şey yapmaz.</summary>
    void Remove(string key);
}
