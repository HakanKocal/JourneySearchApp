using Obilet.Application.Journeys;

namespace Obilet.Web.Validation;

/// <summary>
/// Search Query kurallarını kaynak dosyasındaki metin anahtarlarına eşler.
/// </summary>
/// <remarks>
/// Uygulama katmanı kuralları bilir ama metinleri bilmez; eşleme burada,
/// sunum katmanında yaşar. Böylece kural adları diller arasında sabit
/// kalırken metinler çevrilebilir olur.
/// </remarks>
public static class SearchQueryErrorMessages
{
    /// <summary>
    /// Kuralın kaynak dosyasındaki anahtarını döndürür.
    /// </summary>
    public static string ResourceKeyFor(SearchQueryError error) => error switch
    {
        SearchQueryError.SameOriginAndDestination => "Validation_SameLocation",
        SearchQueryError.DepartureDateInPast => "Validation_PastDate",

        // Yeni bir kural eklenip burası güncellenmezse sessizce yanlış mesaj
        // göstermek yerine açıkça başarısız olsun.
        _ => throw new ArgumentOutOfRangeException(
            nameof(error), error, "Bu kural için bir metin anahtarı tanımlı değil."),
    };

    /// <summary>
    /// Kuralın bağlanacağı form alanının adı.
    /// </summary>
    /// <remarks>
    /// Hata mesajının ilgili alanın yanında görünmesini sağlar. Aynı
    /// lokasyon hatası varış alanına bağlanır, çünkü kullanıcının
    /// değiştireceği alan tipik olarak o.
    /// </remarks>
    public static string FieldFor(SearchQueryError error) => error switch
    {
        SearchQueryError.SameOriginAndDestination => nameof(Models.JourneySearchViewModel.DestinationId),
        SearchQueryError.DepartureDateInPast => nameof(Models.JourneySearchViewModel.DepartureDate),
        _ => string.Empty,
    };
}
