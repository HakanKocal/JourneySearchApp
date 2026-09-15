using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Obilet.Application.Localization;
using Obilet.Application.Locations;
using Obilet.Application.Models;
using Obilet.Application.Sessions;
using Obilet.Infrastructure.Caching;
using Obilet.Tests.Fakes;

namespace Obilet.Tests.Locations;

/// <summary>
/// Lokasyon arama servisinin davranışını doğrular.
/// </summary>
public sealed class LocationServiceSearchTests
{
    private readonly FakeObiletApiClient _apiClient = new();
    private readonly FakeVisitorSessionStore _visitorSession = new();

    private LocationService CreateSut()
    {
        var accessor = new DeviceSessionAccessor(
            _apiClient, _visitorSession, NullLogger<DeviceSessionAccessor>.Instance);

        var executor = new ObiletCallExecutor(
            accessor, NullLogger<ObiletCallExecutor>.Instance);

        IDistributedCache cache = new MemoryDistributedCache(
            new OptionsWrapper<MemoryDistributedCacheOptions>(new MemoryDistributedCacheOptions()));

        return new LocationService(
            _apiClient,
            executor,
            new MarketLocaleResolver(),
            new DistributedLocationCache(cache, NullLogger<DistributedLocationCache>.Instance));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("a")]
    [InlineData(" i ")]
    public async Task Cok_kisa_terimlerde_API_ye_gidilmez(string? query)
    {
        var sut = CreateSut();

        var result = await sut.SearchAsync(query);

        // Tek harf neredeyse her şeyi eşleştiriyor: kullanıcıya yardımcı
        // olmuyor, API'ye ise gereksiz yük bindiriyor.
        Assert.Empty(result);
        Assert.Equal(0, _apiClient.LocationCallCount);
    }

    [Fact]
    public async Task Yeterli_uzunluktaki_terim_API_ye_gider()
    {
        _apiClient.Locations = [new BusLocation(383, "İzmir", 4, null)];
        var sut = CreateSut();

        var result = await sut.SearchAsync("izm");

        Assert.Equal(1, _apiClient.LocationCallCount);
        Assert.Equal("izm", _apiClient.LastLocationQuery);
        Assert.Single(result);
    }

    [Fact]
    public async Task Terim_kirpilarak_gonderilir()
    {
        _apiClient.Locations = [new BusLocation(383, "İzmir", 4, null)];
        var sut = CreateSut();

        await sut.SearchAsync("  izmir  ");

        Assert.Equal("izmir", _apiClient.LastLocationQuery);
    }

    [Fact]
    public async Task Iliskisiz_sonuclar_suzulur()
    {
        // API'nin anlamsız terimdeki gerçek davranışı: popüler lokasyonlara düşüyor.
        _apiClient.Locations =
        [
            new BusLocation(349, "İstanbul Avrupa", 1, "Esenler"),
            new BusLocation(356, "Ankara", 3, "Aşti"),
        ];

        var sut = CreateSut();

        var result = await sut.SearchAsync("xqjz");

        // API çağrısı yapıldı, ama sonuç kullanıcıya gösterilmedi.
        Assert.Equal(1, _apiClient.LocationCallCount);
        Assert.Empty(result);
    }

    [Fact]
    public async Task Arama_sonuclari_onbelleklenmez()
    {
        _apiClient.Locations = [new BusLocation(383, "İzmir", 4, null)];
        var sut = CreateSut();

        await sut.SearchAsync("izmir");
        await sut.SearchAsync("izmir");

        // Terim kuyruğu çok uzun ve isabet oranı düşük; önbelleklemek
        // belleği şişirirdi. Ayrıca varsayılan liste önbelleğini
        // kirletmemesi gerekiyor.
        Assert.Equal(2, _apiClient.LocationCallCount);
    }

    [Fact]
    public async Task Arama_varsayilan_liste_onbellegini_kirletmez()
    {
        _apiClient.Locations = [new BusLocation(383, "İzmir", 4, null)];
        var sut = CreateSut();

        await sut.SearchAsync("izmir");

        _apiClient.Locations =
        [
            new BusLocation(349, "İstanbul Avrupa", 1, null),
            new BusLocation(350, "İstanbul Anadolu", 2, null),
        ];

        var defaults = await sut.GetDefaultAsync();

        // Varsayılan liste arama sonucunu değil kendi çağrısını görmeli.
        Assert.Equal(2, defaults.Count);
        Assert.Equal(349, defaults[0].Id);
    }
}
