using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Obilet.Application.Localization;
using Obilet.Application.Models;
using Obilet.Infrastructure.Caching;
using Obilet.Tests.Fakes;

namespace Obilet.Tests.Caching;

/// <summary>
/// Lokasyon önbelleğinin davranışını doğrular.
/// </summary>
public sealed class DistributedLocationCacheTests
{
    private static readonly IReadOnlyList<BusLocation> TurkishLocations =
    [
        new(349, "İstanbul Avrupa", 1, null),
        new(356, "Ankara", 3, null),
    ];

    private static readonly IReadOnlyList<BusLocation> EnglishLocations =
    [
        new(349, "Istanbul Europe", 1, null),
        new(356, "Ankara", 3, null),
    ];

    private static IDistributedCache RealCache() =>
        new MemoryDistributedCache(
            new OptionsWrapper<MemoryDistributedCacheOptions>(new MemoryDistributedCacheOptions()));

    private static DistributedLocationCache CreateSut(IDistributedCache cache) =>
        new(cache, NullLogger<DistributedLocationCache>.Instance);

    [Fact]
    public async Task Ilk_cagri_uretir_ikinci_cagri_onbellekten_gelir()
    {
        var sut = CreateSut(RealCache());
        var factoryCalls = 0;

        Task<IReadOnlyList<BusLocation>> Factory(CancellationToken _)
        {
            factoryCalls++;
            return Task.FromResult(TurkishLocations);
        }

        var first = await sut.GetOrCreateDefaultAsync(MarketLocale.Turkish, Factory);
        var second = await sut.GetOrCreateDefaultAsync(MarketLocale.Turkish, Factory);

        Assert.Equal(1, factoryCalls);
        Assert.Equal(2, first.Count);
        Assert.Equal("İstanbul Avrupa", second[0].Name);
    }

    [Fact]
    public async Task Farkli_market_locale_ayri_giris_kullanir()
    {
        var sut = CreateSut(RealCache());

        var turkish = await sut.GetOrCreateDefaultAsync(
            MarketLocale.Turkish, _ => Task.FromResult(TurkishLocations));

        var english = await sut.GetOrCreateDefaultAsync(
            MarketLocale.English, _ => Task.FromResult(EnglishLocations));

        // Anahtar Market Locale içermezse, Türkçe adlar İngilizce arayüze
        // sızardı ve dil değiştirme sessizce bozulurdu.
        Assert.Equal("İstanbul Avrupa", turkish[0].Name);
        Assert.Equal("Istanbul Europe", english[0].Name);
    }

    [Fact]
    public async Task Onbellege_alinan_giris_diger_locale_i_etkilemez()
    {
        var sut = CreateSut(RealCache());
        var englishFactoryCalls = 0;

        await sut.GetOrCreateDefaultAsync(
            MarketLocale.Turkish, _ => Task.FromResult(TurkishLocations));

        await sut.GetOrCreateDefaultAsync(MarketLocale.English, _ =>
        {
            englishFactoryCalls++;
            return Task.FromResult(EnglishLocations);
        });

        // Türkçe girişin varlığı İngilizce isteği karşılamamalı.
        Assert.Equal(1, englishFactoryCalls);
    }

    [Fact]
    public async Task Bos_sonuc_onbellege_alinmaz()
    {
        var sut = CreateSut(RealCache());
        var factoryCalls = 0;

        Task<IReadOnlyList<BusLocation>> EmptyFactory(CancellationToken _)
        {
            factoryCalls++;
            return Task.FromResult<IReadOnlyList<BusLocation>>([]);
        }

        await sut.GetOrCreateDefaultAsync(MarketLocale.Turkish, EmptyFactory);
        await sut.GetOrCreateDefaultAsync(MarketLocale.Turkish, EmptyFactory);

        // Geçici bir API sorunu, yarım saat boyunca boş bir lokasyon
        // listesine dönüşmemeli.
        Assert.Equal(2, factoryCalls);
    }

    [Fact]
    public async Task Onbellege_ulasilamadiginda_uygulama_calismaya_devam_eder()
    {
        var sut = CreateSut(new ThrowingDistributedCache());

        var result = await sut.GetOrCreateDefaultAsync(
            MarketLocale.Turkish, _ => Task.FromResult(TurkishLocations));

        // Önbellek bir kolaylıktır, bir bağımlılık değil: Redis düştüğünde
        // uygulama yavaşlar, ama çalışmaya devam eder.
        Assert.Equal(2, result.Count);
        Assert.Equal("İstanbul Avrupa", result[0].Name);
    }
}
