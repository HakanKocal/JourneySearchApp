using Microsoft.Extensions.Logging.Abstractions;
using Obilet.Application.Sessions;
using Obilet.Tests.Fakes;

namespace Obilet.Tests.Sessions;

/// <summary>
/// Device Session'ın ziyaretçi başına oluşturulup yeniden kullanıldığını doğrular.
/// </summary>
public sealed class DeviceSessionAccessorTests
{
    private readonly FakeObiletApiClient _apiClient = new();
    private readonly FakeVisitorSessionStore _visitorSession = new();

    private DeviceSessionAccessor CreateSut() => new(
        _apiClient,
        _visitorSession,
        NullLogger<DeviceSessionAccessor>.Instance);

    [Fact]
    public async Task Oturum_yoksa_olusturulur()
    {
        var sut = CreateSut();

        var session = await sut.GetOrCreateAsync();

        Assert.Equal(1, _apiClient.CreateSessionCallCount);
        Assert.True(session.IsUsable);
    }

    [Fact]
    public async Task Oturum_varsa_yeniden_kullanilir_ve_API_tekrar_cagrilmaz()
    {
        var sut = CreateSut();

        var first = await sut.GetOrCreateAsync();
        var second = await sut.GetOrCreateAsync();
        var third = await sut.GetOrCreateAsync();

        // Şartname her ziyaretçinin kendi oturumunu kullanmasını istiyor;
        // her istekte yeni oturum açmak hem gereksiz yük hem de gereksinimin
        // yanlış okunması olurdu.
        Assert.Equal(1, _apiClient.CreateSessionCallCount);
        Assert.Equal(first, second);
        Assert.Equal(first, third);
    }

    [Fact]
    public async Task Yenileme_yeni_bir_oturum_olusturur()
    {
        var sut = CreateSut();

        var original = await sut.GetOrCreateAsync();
        var refreshed = await sut.RefreshAsync();

        Assert.Equal(2, _apiClient.CreateSessionCallCount);
        Assert.NotEqual(original, refreshed);
    }

    [Fact]
    public async Task Yenilemeden_sonra_yeni_oturum_yeniden_kullanilir()
    {
        var sut = CreateSut();

        await sut.GetOrCreateAsync();
        var refreshed = await sut.RefreshAsync();
        var subsequent = await sut.GetOrCreateAsync();

        // Yenileme sonrası depo tutarlı kalmalı: aksi hâlde her istek
        // oturumu bir kez daha yenileyerek API'yi gereksiz yorar.
        Assert.Equal(2, _apiClient.CreateSessionCallCount);
        Assert.Equal(refreshed, subsequent);
    }

    [Fact]
    public async Task Depoda_eksik_kayit_varsa_oturum_yeniden_olusturulur()
    {
        var sut = CreateSut();
        await sut.GetOrCreateAsync();

        // Çiftin yalnızca bir yarısının kalması kullanılamaz bir durumdur;
        // yarım bir kimlikle API'ye gitmek yerine yeniden oluşturulmalı.
        _visitorSession.Remove("obilet:device-session:device-id");

        var session = await sut.GetOrCreateAsync();

        Assert.Equal(2, _apiClient.CreateSessionCallCount);
        Assert.True(session.IsUsable);
    }
}
