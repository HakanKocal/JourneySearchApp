using Microsoft.Extensions.Logging.Abstractions;
using Obilet.Application.Exceptions;
using Obilet.Application.Models;
using Obilet.Application.Sessions;
using Obilet.Tests.Fakes;

namespace Obilet.Tests.Sessions;

/// <summary>
/// Oturum geçersizleştiğinde isteğin bir kez tekrarlandığını doğrular.
/// </summary>
/// <remarks>
/// Bu davranış kritik, çünkü API geçersiz bir Device Session'ı HTTP 400 ve
/// gövdede özel bir durum değeriyle reddediyor. Yeniden deneme olmadan,
/// oturumu düşen bir kullanıcı hata sayfası görürdü.
/// </remarks>
public sealed class ObiletCallExecutorTests
{
    private readonly FakeObiletApiClient _apiClient = new();
    private readonly FakeVisitorSessionStore _visitorSession = new();

    private ObiletCallExecutor CreateSut()
    {
        var accessor = new DeviceSessionAccessor(
            _apiClient,
            _visitorSession,
            NullLogger<DeviceSessionAccessor>.Instance);

        return new ObiletCallExecutor(accessor, NullLogger<ObiletCallExecutor>.Instance);
    }

    private static ObiletApiException SessionExpired() => new(
        ObiletApiException.DeviceSessionError,
        endpoint: "location/getbuslocations",
        upstreamMessage: null,
        correlationId: "test");

    [Fact]
    public async Task Basarili_cagri_oldugu_gibi_dondurulur()
    {
        var sut = CreateSut();
        var attempts = 0;

        var result = await sut.ExecuteAsync((_, _) =>
        {
            attempts++;
            return Task.FromResult("tamam");
        });

        Assert.Equal("tamam", result);
        Assert.Equal(1, attempts);
        Assert.Equal(1, _apiClient.CreateSessionCallCount);
    }

    [Fact]
    public async Task Oturum_gecersizse_yenilenir_ve_istek_bir_kez_tekrarlanir()
    {
        var sut = CreateSut();
        var attempts = 0;
        var sessionsSeen = new List<DeviceSession>();

        var result = await sut.ExecuteAsync((session, _) =>
        {
            attempts++;
            sessionsSeen.Add(session);

            if (attempts == 1)
            {
                throw SessionExpired();
            }

            return Task.FromResult("tamam");
        });

        Assert.Equal("tamam", result);
        Assert.Equal(2, attempts);

        // İkinci deneme yenilenmiş oturumu kullanmalı; aynı oturumla tekrar
        // denemek aynı hatayı almaktan başka bir şey yapmazdı.
        Assert.Equal(2, _apiClient.CreateSessionCallCount);
        Assert.NotEqual(sessionsSeen[0], sessionsSeen[1]);
    }

    [Fact]
    public async Task Yenilemeden_sonra_da_basarisizsa_hata_yukari_kabarir()
    {
        var sut = CreateSut();
        var attempts = 0;

        var exception = await Assert.ThrowsAsync<ObiletApiException>(() =>
            sut.ExecuteAsync<string>((_, _) =>
            {
                attempts++;
                throw SessionExpired();
            }));

        // Sınırsız yeniden deneme, API tarafında kalıcı bir sorun varsa
        // sonsuz döngüye dönerdi. Tam olarak iki deneme yapılır.
        Assert.Equal(2, attempts);
        Assert.Equal(ObiletApiException.DeviceSessionError, exception.Status);
    }

    [Fact]
    public async Task Oturumla_ilgisiz_hatalar_tekrarlanmaz()
    {
        var sut = CreateSut();
        var attempts = 0;

        var exception = await Assert.ThrowsAsync<ObiletApiException>(() =>
            sut.ExecuteAsync<string>((_, _) =>
            {
                attempts++;
                throw new ObiletApiException(
                    "InvalidRoute",
                    endpoint: "journey/getbusjourneys",
                    upstreamMessage: null,
                    correlationId: null);
            }));

        // Geçersiz güzergâh oturumla ilgili değil; tekrar denemek yalnızca
        // API'ye gereksiz bir istek daha gönderirdi.
        Assert.Equal(1, attempts);
        Assert.Equal("InvalidRoute", exception.Status);
        Assert.Equal(1, _apiClient.CreateSessionCallCount);
    }
}
