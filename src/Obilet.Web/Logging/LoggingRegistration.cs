using Elastic.Channels;
using Elastic.Ingest.Elasticsearch;
using Elastic.Ingest.Elasticsearch.DataStreams;
using Elastic.Serilog.Sinks;
using Serilog;
using Serilog.Events;

namespace Obilet.Web.Logging;

/// <summary>
/// Uygulama günlüklerini yapılandırır.
/// </summary>
public static class LoggingRegistration
{
    /// <summary>
    /// Yapılandırmada aranan Elasticsearch bağlantı dizesinin adı.
    /// </summary>
    public const string ElasticsearchConnectionStringName = "Elasticsearch";

    /// <summary>
    /// Günlüklerin yazıldığı veri akışının adı.
    /// </summary>
    private const string DataStreamName = "obilet-web";

    /// <summary>
    /// Konsol ve — yapılandırıldıysa — Elasticsearch günlüklemesini kaydeder.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Elasticsearch bilinçli olarak <b>zorunlu bir bağımlılık değildir</b>.
    /// Bağlantı dizesi yoksa yalnızca konsola yazılır ve uygulama hiçbir ek
    /// kurulum gerektirmeden çalışır. Aynı gerekçe Redis için de geçerli;
    /// bkz. docs/adr/0003.
    /// </para>
    /// <para>
    /// Günlükler süreç dışına çıktığı için ne yazıldığı daha da önemli hâle
    /// geliyor. İki koruma var: Device Session ve ApiClientToken hiçbir günlük
    /// çağrısına girmiyor, ve <c>Microsoft.Extensions.Http</c> kategorisi
    /// uyarı seviyesine çekiliyor — o kategori düşük seviyelerde istek
    /// başlıklarını yazabiliyor ve <c>Authorization</c> başlığı orada.
    /// </para>
    /// </remarks>
    public static void AddObiletLogging(this WebApplicationBuilder builder)
    {
        var elasticsearchUrl = builder.Configuration
            .GetConnectionString(ElasticsearchConnectionStringName);

        // Varsayılan sağlayıcılar kaldırılır. AddSerilog onları kendiliğinden
        // kaldırmıyor; bırakıldığında her kayıt iki kez, iki farklı biçimde
        // konsola yazılıyor.
        builder.Logging.ClearProviders();

        builder.Services.AddSerilog((services, configuration) =>
        {
            configuration
                .ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()

                // Kibana'da birden fazla uygulama varsa ayırt edilebilsin.
                .Enrich.WithProperty("service.name", DataStreamName)
                .Enrich.WithProperty("service.environment", builder.Environment.EnvironmentName)

                // İstek başlıklarını yazabilen kategori; Authorization başlığı
                // orada olduğu için uyarı seviyesinin altına inmesine izin verilmiyor.
                .MinimumLevel.Override("Microsoft.Extensions.Http", LogEventLevel.Warning)
                .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)

                // Çerçevenin kendi istek günlükleri bastırılıyor.
                //
                // Ölçüm: bastırılmadan önce 178 kaydın yalnızca 2'si uygulama
                // kodundan geliyordu; gerisi her istek için üç dört satır
                // yazan çerçeve kategorileriydi (Hosting.Diagnostics,
                // EndpointMiddleware, ControllerActionInvoker...). Zaten
                // UseSerilogRequestLogging ile istek başına tek bir özet satırı
                // yazıyoruz; ikisi birlikte aynı bilgiyi dört kez kaydediyordu.
                //
                // Uyarı ve hatalar etkilenmiyor: seviye yalnızca Information
                // ve altını susturuyor, dolayısıyla çerçeveden gelen gerçek
                // sorunlar görünmeye devam ediyor.
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)

                .WriteTo.Console();

            if (string.IsNullOrWhiteSpace(elasticsearchUrl))
            {
                return;
            }

            configuration.WriteTo.Elasticsearch(
                [new Uri(elasticsearchUrl)],
                options =>
                {
                    // Veri akışı (data stream) kullanılıyor: günlükler zamana
                    // göre bölünen indislere yazılır ve saklama politikası
                    // Elasticsearch tarafında yönetilebilir.
                    options.DataStream = new DataStreamName("logs", DataStreamName);

                    // Şablonu uygulama kendisi kurar; böylece Kibana'da alanlar
                    // ilk günlükten itibaren doğru tiplerle görünür.
                    options.BootstrapMethod = BootstrapMethod.Silent;

                    options.ConfigureChannel = channel =>
                    {
                        // Elasticsearch erişilemezse uygulama durmaz: kanal
                        // tamponlar, yeniden dener ve dolduğunda en eskiyi atar.
                        // Günlükleme bir kolaylık, bir bağımlılık değil.
                        channel.BufferOptions = new BufferOptions
                        {
                            OutboundBufferMaxSize = 1_000,
                            OutboundBufferMaxLifetime = TimeSpan.FromSeconds(5),
                        };
                    };
                });
        });
    }

    /// <summary>
    /// Hangi günlük hedeflerinin etkin olduğunu açılışta bildirir.
    /// </summary>
    /// <remarks>
    /// Sessiz bir yedeğe düşmek, Kibana'da günlük beklerken hiçbir şey
    /// görmemeye yol açar. Açılış satırı bunu görünür kılıyor.
    /// </remarks>
    public static void LogLoggingTargets(this WebApplication app)
    {
        var url = app.Configuration.GetConnectionString(ElasticsearchConnectionStringName);

        var logger = app.Services
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("Obilet.Startup");

        if (string.IsNullOrWhiteSpace(url))
        {
            logger.LogInformation(
                "Günlük hedefi: yalnızca konsol (Elasticsearch bağlantı dizesi tanımlı değil).");
        }
        else
        {
            logger.LogInformation(
                "Günlük hedefleri: konsol ve Elasticsearch ({DataStream} veri akışı).",
                $"logs-{DataStreamName}-default");
        }
    }
}
