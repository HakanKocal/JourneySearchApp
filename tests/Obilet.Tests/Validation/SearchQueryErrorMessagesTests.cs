using Obilet.Application.Journeys;
using Obilet.Web.Validation;

namespace Obilet.Tests.Validation;

/// <summary>
/// Her kuralın bir metin anahtarına ve bir form alanına eşlendiğini doğrular.
/// </summary>
public sealed class SearchQueryErrorMessagesTests
{
    [Fact]
    public void Her_kural_icin_bir_metin_anahtari_tanimli()
    {
        // Yeni bir kural eklenip eşleme güncellenmezse bu test kırılır;
        // aksi hâlde kullanıcıya sessizce yanlış veya eksik mesaj gösterilirdi.
        foreach (var error in Enum.GetValues<SearchQueryError>())
        {
            var key = SearchQueryErrorMessages.ResourceKeyFor(error);

            Assert.False(string.IsNullOrWhiteSpace(key));
        }
    }

    [Fact]
    public void Her_kural_icin_bir_form_alani_tanimli()
    {
        foreach (var error in Enum.GetValues<SearchQueryError>())
        {
            var field = SearchQueryErrorMessages.FieldFor(error);

            Assert.False(string.IsNullOrWhiteSpace(field));
        }
    }

    [Fact]
    public void Metin_anahtarlari_birbirinden_farkli()
    {
        var keys = Enum.GetValues<SearchQueryError>()
            .Select(SearchQueryErrorMessages.ResourceKeyFor)
            .ToList();

        Assert.Equal(keys.Count, keys.Distinct().Count());
    }

    [Fact]
    public void Kural_adlari_tasinabilir_bicimde_cozulur()
    {
        // Sefer sayfasından arama formuna hatalar kural adı olarak taşınıyor.
        // Bu gidiş-dönüşün bozulması, kullanıcının hiçbir mesaj görmemesine
        // yol açardı.
        foreach (var error in Enum.GetValues<SearchQueryError>())
        {
            var parsed = Enum.TryParse<SearchQueryError>(error.ToString(), out var result);

            Assert.True(parsed);
            Assert.Equal(error, result);
        }
    }
}
