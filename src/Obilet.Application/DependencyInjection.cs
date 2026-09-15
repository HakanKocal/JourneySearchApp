using Microsoft.Extensions.DependencyInjection;
using Obilet.Application.Abstractions;
using Obilet.Application.Journeys;
using Obilet.Application.Localization;
using Obilet.Application.Locations;
using Obilet.Application.Sessions;
using Obilet.Application.Time;

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
    /// Device Session'a bağlı olanlar <c>Scoped</c>: oturum tek bir
    /// ziyaretçiye özeldir ve istek ömrü boyunca aynı kalmalıdır.
    /// </remarks>
    public static IServiceCollection AddObiletApplication(this IServiceCollection services)
    {
        services.AddScoped<IDeviceSessionAccessor, DeviceSessionAccessor>();
        services.AddScoped<IObiletCallExecutor, ObiletCallExecutor>();
        services.AddScoped<ILocationService, LocationService>();
        services.AddScoped<IJourneyService, JourneyService>();

        // Durumsuz: yalnızca ambient kültürü okuyup beyaz listeden geçirir.
        services.AddSingleton<IMarketLocaleResolver, MarketLocaleResolver>();

        // Pazarın saat dilimi çözümlemesi bir kez yapılır.
        services.AddSingleton<IMarketClock, MarketClock>();

        return services;
    }
}
