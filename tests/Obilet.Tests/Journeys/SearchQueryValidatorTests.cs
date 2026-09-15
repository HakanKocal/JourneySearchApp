using Obilet.Application.Journeys;

namespace Obilet.Tests.Journeys;

/// <summary>
/// Search Query kurallarını doğrular.
/// </summary>
/// <remarks>
/// Bu kuralların sunucu tarafında uygulanması zorunlu, çünkü canlı testte
/// obilet API'sinin hiçbirini güvenilir biçimde uygulamadığı görüldü:
/// geçmiş bir tarih için 150 sefer döndürdü ve geçersiz bir lokasyon
/// kimliğini hata olarak bildirmek yerine boş sonuç verdi.
/// </remarks>
public sealed class SearchQueryValidatorTests
{
    private static readonly DateOnly Today = new(2026, 9, 15);

    [Fact]
    public void Gecerli_sorgu_hata_uretmez()
    {
        var errors = SearchQueryValidator.Validate(
            originId: 349, destinationId: 356, departureDate: Today.AddDays(1), today: Today);

        Assert.Empty(errors);
        Assert.True(SearchQueryValidator.IsValid(349, 356, Today.AddDays(1), Today));
    }

    [Fact]
    public void Ayni_lokasyon_reddedilir()
    {
        var errors = SearchQueryValidator.Validate(349, 349, Today.AddDays(1), Today);

        Assert.Contains(SearchQueryError.SameOriginAndDestination, errors);
    }

    [Fact]
    public void Gecmis_tarih_reddedilir()
    {
        var errors = SearchQueryValidator.Validate(349, 356, Today.AddDays(-1), Today);

        Assert.Contains(SearchQueryError.DepartureDateInPast, errors);
    }

    [Fact]
    public void Bugun_gecerli_bir_tarihtir()
    {
        // Şartname minimum geçerli tarihi bugün olarak belirliyor; sınır
        // bugünün kendisidir, yarın değil. Bu, kolayca yanlış yazılan bir
        // karşılaştırma (<= yerine <) olduğu için ayrı bir test hak ediyor.
        var errors = SearchQueryValidator.Validate(349, 356, Today, Today);

        Assert.Empty(errors);
    }

    [Fact]
    public void Uzak_gelecek_tarihi_kabul_edilir()
    {
        var errors = SearchQueryValidator.Validate(349, 356, Today.AddYears(1), Today);

        Assert.Empty(errors);
    }

    [Fact]
    public void Iki_kural_birlikte_ihlal_edilebilir()
    {
        var errors = SearchQueryValidator.Validate(349, 349, Today.AddDays(-5), Today);

        // Kullanıcıya iki sorunu birden göstermek, birini düzeltip diğerine
        // takılmasından daha yardımcı.
        Assert.Equal(2, errors.Count);
        Assert.Contains(SearchQueryError.SameOriginAndDestination, errors);
        Assert.Contains(SearchQueryError.DepartureDateInPast, errors);
    }

    [Fact]
    public void Dun_ile_bugun_arasindaki_sinir_dogru()
    {
        Assert.False(SearchQueryValidator.IsValid(349, 356, Today.AddDays(-1), Today));
        Assert.True(SearchQueryValidator.IsValid(349, 356, Today, Today));
    }
}
