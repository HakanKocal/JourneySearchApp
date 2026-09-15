using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Obilet.Application.Abstractions;
using Obilet.Infrastructure.Obilet;

namespace Obilet.Infrastructure;

/// <summary>
/// Infrastructure katmanının servis kayıtları.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// obilet API istemcisini ve yapılandırmasını kaydeder.
    /// </summary>
    public static IServiceCollection AddObiletInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<ObiletApiOptions>()
            .Bind(configuration.GetSection(ObiletApiOptions.SectionName))
            .ValidateDataAnnotations()
            // Eksik yapılandırma ilk API çağrısında değil, uygulama açılışında
            // fark edilsin: sessizce yanlış çalışmak yerine hemen başarısız olur.
            .ValidateOnStart();

        services.AddHttpClient<IObiletApiClient, ObiletApiClient>(ConfigureHttpClient);

        return services;
    }

    private static void ConfigureHttpClient(IServiceProvider provider, HttpClient client)
    {
        var options = provider.GetRequiredService<IOptions<ObiletApiOptions>>().Value;

        client.BaseAddress = new Uri(
            options.BaseUrl.EndsWith('/') ? options.BaseUrl : options.BaseUrl + "/");

        // Açıkça ayarlanması zorunlu: API tanımadığı bir Market Locale
        // değerinde isteği süresiz askıda bırakıyor ve 100 saniyelik
        // varsayılan bu durumda istek yığılmasına yol açar.
        client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", options.ApiClientToken);
    }
}
