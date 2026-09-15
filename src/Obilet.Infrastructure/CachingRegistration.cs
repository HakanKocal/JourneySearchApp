using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Obilet.Application.Caching;
using Obilet.Infrastructure.Caching;
using StackExchange.Redis;

namespace Obilet.Infrastructure;

/// <summary>
/// Dağıtık önbellek kayıtları.
/// </summary>
public static class CachingRegistration
{
    /// <summary>
    /// Yapılandırmada aranan Redis bağlantı dizesinin adı.
    /// </summary>
    public const string RedisConnectionStringName = "Redis";

    /// <summary>
    /// Dağıtık önbelleği ve lokasyon önbelleğini kaydeder.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Redis bağlantı dizesi varsa Redis, yoksa süreç içi bellek kaydedilir.
    /// Uygulama kodu her iki durumda da yalnızca
    /// <c>IDistributedCache</c> arayüzünü tanıdığı için değişmez.
    /// </para>
    /// <para>
    /// Redis bilinçli olarak <b>zorunlu bir bağımlılık değildir</b>: projeyi
    /// klonlayan birinin hiçbir kurulum yapmadan çalıştırabilmesi gerekiyor.
    /// Gerekçe için bkz. docs/adr/0003.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddObiletCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetConnectionString(RedisConnectionStringName);

        if (string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddDistributedMemoryCache();
        }
        else
        {
            services.AddStackExchangeRedisCache(options =>
            {
                var configurationOptions = ConfigurationOptions.Parse(redisConnectionString);

                // Redis henüz hazır değilken uygulamanın açılışta ölmesini
                // engeller: bağlantı arka planda yeniden denenir. Konteyner
                // sıralamasında Redis uygulamadan sonra hazır olabiliyor.
                configurationOptions.AbortOnConnectFail = false;

                options.ConfigurationOptions = configurationOptions;

                // Anahtarların hangi uygulamaya ait olduğu, paylaşılan bir
                // Redis örneğinde ayırt edilebilsin.
                options.InstanceName = "obilet:";
            });
        }

        services.AddSingleton<ILocationCache, DistributedLocationCache>();

        return services;
    }

    /// <summary>
    /// Hangi önbellek sağlayıcısının seçildiğini açılışta loglar.
    /// </summary>
    /// <remarks>
    /// Sessiz bir yedeğe düşmek, Redis'in çalıştığını sanırken süreç içi
    /// bellekle çalışmaya yol açabilir. Açılış logu bunu görünür kılar.
    /// </remarks>
    public static void LogCacheProvider(IConfiguration configuration, ILogger logger)
    {
        var redisConnectionString = configuration.GetConnectionString(RedisConnectionStringName);

        if (string.IsNullOrWhiteSpace(redisConnectionString))
        {
            logger.LogInformation(
                "Dağıtık önbellek sağlayıcısı: süreç içi bellek (Redis bağlantı dizesi tanımlı değil).");
        }
        else
        {
            logger.LogInformation("Dağıtık önbellek sağlayıcısı: Redis.");
        }
    }
}
