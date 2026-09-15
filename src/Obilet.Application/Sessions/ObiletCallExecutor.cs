using Microsoft.Extensions.Logging;
using Obilet.Application.Abstractions;
using Obilet.Application.Exceptions;
using Obilet.Application.Models;

namespace Obilet.Application.Sessions;

/// <inheritdoc cref="IObiletCallExecutor"/>
public sealed class ObiletCallExecutor : IObiletCallExecutor
{
    private readonly IDeviceSessionAccessor _deviceSessions;
    private readonly ILogger<ObiletCallExecutor> _logger;

    public ObiletCallExecutor(
        IDeviceSessionAccessor deviceSessions,
        ILogger<ObiletCallExecutor> logger)
    {
        _deviceSessions = deviceSessions;
        _logger = logger;
    }

    public async Task<TResult> ExecuteAsync<TResult>(
        Func<DeviceSession, CancellationToken, Task<TResult>> call,
        CancellationToken cancellationToken = default)
    {
        var session = await _deviceSessions.GetOrCreateAsync(cancellationToken);

        try
        {
            return await call(session, cancellationToken);
        }
        catch (ObiletApiException ex) when (ex.IsRecoverableBySessionRefresh)
        {
            // Oturum API tarafında düşmüş. Yeni bir tane alıp yalnızca bir kez
            // daha deniyoruz; ikinci başarısızlık gerçek bir hatadır ve yukarı
            // kabarır. Sınırsız yeniden deneme, API'de kalıcı bir sorun
            // varsa sonsuz döngüye dönerdi.
            _logger.LogWarning(
                "Device Session geçersiz sayıldı, yenilenip istek tekrarlanıyor. CorrelationId: {CorrelationId}",
                ex.CorrelationId);

            var refreshed = await _deviceSessions.RefreshAsync(cancellationToken);
            return await call(refreshed, cancellationToken);
        }
    }
}
