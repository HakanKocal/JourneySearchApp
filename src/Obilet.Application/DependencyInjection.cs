using Microsoft.Extensions.DependencyInjection;
using Obilet.Application.Abstractions;
using Obilet.Application.Locations;
using Obilet.Application.Sessions;

namespace Obilet.Application;

/// <summary>
/// Uygulama katmanının servis kayıtları.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Uygulama servislerini kaydeder.
    /// </summary>
    /// <remarks>
    /// Tümü <c>Scoped</c>: Device Session tek bir ziyaretçiye özeldir ve
    /// istek ömrü boyunca aynı kalmalıdır.
    /// </remarks>
    public static IServiceCollection AddObiletApplication(this IServiceCollection services)
    {
        services.AddScoped<IDeviceSessionAccessor, DeviceSessionAccessor>();
        services.AddScoped<IObiletCallExecutor, ObiletCallExecutor>();
        services.AddScoped<ILocationService, LocationService>();

        return services;
    }
}
