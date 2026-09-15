namespace Obilet.Application.Journeys;

/// <summary>
/// Bir Search Query'nin ihlal edebileceği kurallar.
/// </summary>
public enum SearchQueryError
{
    /// <summary>Aynı Bus Location hem Origin hem Destination olarak seçilmiş.</summary>
    SameOriginAndDestination,

    /// <summary>Departure Date bugünden önce.</summary>
    DepartureDateInPast,
}

/// <summary>
/// Search Query kurallarını uygular.
/// </summary>
/// <remarks>
/// <para>
/// Kurallar bilinçli olarak view model'in içine değil, paylaşılan bir yere
/// konuldu. Sebebi şu: sefer sayfası kendi adresine sahip ve elle
/// yazılabiliyor, yani doğrulamanın <b>iki farklı giriş noktasında</b>
/// uygulanması gerekiyor — form gönderimi ve adres üzerinden erişim. Adres
/// üzerinden gelen istek view model'e bağlanmadığı için
/// <c>IValidatableObject</c> o yolu hiç görmezdi.
/// </para>
/// <para>
/// Sunucu tarafı doğrulama pazarlık konusu değil: API bu kuralların hiçbirini
/// güvenilir biçimde uygulamıyor. Canlı testte geçmiş bir tarih için 150
/// sefer döndürdü ve geçersiz bir lokasyon kimliğini hata olarak bildirmek
/// yerine boş sonuç verdi.
/// </para>
/// </remarks>
public static class SearchQueryValidator
{
    /// <summary>
    /// Search Query'yi doğrular ve ihlal edilen kuralları döndürür.
    /// Boş liste, sorgunun geçerli olduğu anlamına gelir.
    /// </summary>
    /// <param name="originId">Seçilen kalkış lokasyonunun kimliği.</param>
    /// <param name="destinationId">Seçilen varış lokasyonunun kimliği.</param>
    /// <param name="departureDate">Seçilen kalkış günü.</param>
    /// <param name="today">
    /// Bugünün tarihi. Dışarıdan verilir; aksi hâlde tarihe bağlı davranış
    /// test edilemez hâle gelir.
    /// </param>
    public static IReadOnlyList<SearchQueryError> Validate(
        int originId,
        int destinationId,
        DateOnly departureDate,
        DateOnly today)
    {
        var errors = new List<SearchQueryError>(capacity: 2);

        if (originId == destinationId)
        {
            errors.Add(SearchQueryError.SameOriginAndDestination);
        }

        // Bugün geçerli bir tarihtir; sınır bugünün kendisidir, yarın değil.
        if (departureDate < today)
        {
            errors.Add(SearchQueryError.DepartureDateInPast);
        }

        return errors;
    }

    /// <summary>
    /// Search Query'nin geçerli olup olmadığını söyler.
    /// </summary>
    public static bool IsValid(
        int originId,
        int destinationId,
        DateOnly departureDate,
        DateOnly today)
        => Validate(originId, destinationId, departureDate, today).Count == 0;
}
