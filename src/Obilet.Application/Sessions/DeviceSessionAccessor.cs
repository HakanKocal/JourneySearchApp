using Microsoft.Extensions.Logging;
using Obilet.Application.Abstractions;
using Obilet.Application.Models;

namespace Obilet.Application.Sessions;

/// <inheritdoc cref="IDeviceSessionAccessor"/>
public sealed class DeviceSessionAccessor : IDeviceSessionAccessor
{
    // Visitor Session içindeki depolama anahtarları.
    private const string SessionIdKey = "obilet:device-session:session-id";
    private const string DeviceIdKey = "obilet:device-session:device-id";

    private readonly IObiletApiClient _apiClient;
    private readonly IVisitorSessionStore _visitorSession;
    private readonly ILogger<DeviceSessionAccessor> _logger;

    public DeviceSessionAccessor(
        IObiletApiClient apiClient,
        IVisitorSessionStore visitorSession,
        ILogger<DeviceSessionAccessor> logger)
    {
        _apiClient = apiClient;
        _visitorSession = visitorSession;
        _logger = logger;
    }

    public async Task<DeviceSession> GetOrCreateAsync(CancellationToken cancellationToken = default)
    {
        var existing = ReadFromVisitorSession();
        if (existing is not null)
        {
            return existing;
        }

        return await CreateAndStoreAsync(cancellationToken);
    }

    public async Task<DeviceSession> RefreshAsync(CancellationToken cancellationToken = default)
    {
        _visitorSession.Remove(SessionIdKey);
        _visitorSession.Remove(DeviceIdKey);

        return await CreateAndStoreAsync(cancellationToken);
    }

    private DeviceSession? ReadFromVisitorSession()
    {
        var sessionId = _visitorSession.Get(SessionIdKey);
        var deviceId = _visitorSession.Get(DeviceIdKey);

        if (string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(deviceId))
        {
            return null;
        }

        return new DeviceSession(sessionId, deviceId);
    }

    private async Task<DeviceSession> CreateAndStoreAsync(CancellationToken cancellationToken)
    {
        var session = await _apiClient.CreateSessionAsync(cancellationToken);

        _visitorSession.Set(SessionIdKey, session.SessionId);
        _visitorSession.Set(DeviceIdKey, session.DeviceId);

        // Kimlik bilgisinin kendisi asla loglanmaz; yalnızca oluşturulduğu bilgisi.
        _logger.LogInformation("Yeni bir Device Session oluşturuldu.");

        return session;
    }
}
