using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Obilet.Application;
using Obilet.Application.Abstractions;
using Obilet.Application.Localization;
using Obilet.Infrastructure;
using Obilet.Web.Filters;
using Obilet.Web.Logging;
using Obilet.Web.Sessions;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// --- Günlükleme --------------------------------------------------------------
// Konsol her zaman; Elasticsearch yalnızca bağlantı dizesi tanımlıysa.
// Elasticsearch zorunlu bir bağımlılık değil — Redis'teki gerekçenin aynısı,
// bkz. docs/adr/0003 — ve erişilemediğinde uygulama günlükleri tamponlayıp
// çalışmaya devam eder.
builder.AddObiletLogging();

// --- Lokalizasyon -----------------------------------------------------------
// Kaynak dosyaları Resources klasöründe; nötr dosya Türkçe metinleri taşır.
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services
    .AddControllersWithViews(options =>
    {
        // API hatalarını tek bir yerde kullanıcıya gösterilebilir yanıta
        // çevirir. Upstream detayın loglanıp asla render edilmemesi burada
        // garanti altına alınıyor; API başarısızlıkta kendi yığın izini
        // döndürüyor.
        options.Filters.Add<ObiletApiExceptionFilter>();
    })
    .AddViewLocalization();

// Desteklenen kültüreler, API'ye gönderilmesine izin verilen Market Locale
// değerlerinden türetilir. Böylece arayüzde seçilebilen bir dilin API
// karşılığı olmaması gibi bir durum ortaya çıkamaz.
var supportedCultures = MarketLocale.Supported
    .Select(locale => new CultureInfo(locale))
    .ToArray();

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new RequestCulture(MarketLocale.Default);
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;

    // Desteklenmeyen bir değer geldiğinde sessizce varsayılana düşülür.
    // Bu yalnızca bir kolaylık değil, bir koruma: API tanımadığı bir
    // Market Locale değerinde hata döndürmek yerine isteği süresiz askıda
    // bırakıyor. Bkz. docs/adr/0002.
    options.RequestCultureProviders =
    [
        new QueryStringRequestCultureProvider(),
        new CookieRequestCultureProvider(),
        new AcceptLanguageHeaderRequestCultureProvider(),
    ];
});

// --- Dağıtık önbellek --------------------------------------------------------
// Redis bağlantı dizesi varsa Redis, yoksa süreç içi bellek. Visitor Session
// deposu da bu önbelleğin üzerine kurulduğu için Redis yapılandırıldığında
// oturumlar kendiliğinden instance'lar arası paylaşılır hâle gelir.
// Redis zorunlu bir bağımlılık değildir; bkz. docs/adr/0003.
builder.Services.AddObiletCaching(builder.Configuration);

// --- Visitor Session ---------------------------------------------------------
// Device Session sunucu tarafında, ziyaretçiye özel olarak saklanır.
builder.Services.AddSession(options =>
{
    options.Cookie.Name = ".Obilet.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IVisitorSessionStore, HttpVisitorSessionStore>();

// Konteyner sağlık kontrolü için. Bilinçli olarak yalnızca uygulamanın ayakta
// olduğunu bildirir; obilet API'sini veya Redis'i yoklamaz. Üçüncü parti bir
// servise bağlı bir sağlık kontrolü, o servis yavaşladığında veya hız sınırı
// uyguladığında konteynerin gereksizce yeniden başlatılmasına yol açardı —
// oysa uygulama önbellekle veya hata sayfasıyla hizmet vermeye devam edebilir.
builder.Services.AddHealthChecks();

// Tarihe bağlı davranış (varsayılan kalkış tarihi, geçmiş tarih kontrolü)
// doğrudan DateTime.Today yerine bu soyutlamadan okunur; aksi hâlde test
// edilemez hâle gelir.
builder.Services.AddSingleton(TimeProvider.System);

// --- Katmanlar --------------------------------------------------------------
builder.Services.AddObiletApplication();
builder.Services.AddObiletInfrastructure(builder.Configuration);

var app = builder.Build();

// Hangi önbellek sağlayıcısının seçildiği açılışta görünür olsun: sessiz bir
// yedeğe düşmek, Redis çalışıyor sanılırken süreç içi bellekle çalışmaya yol açar.
CachingRegistration.LogCacheProvider(
    app.Configuration,
    app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Obilet.Startup"));

app.LogLoggingTargets();

// Her isteği tek bir özet satırıyla günlükler; her istek için üç ayrı
// satır yazan varsayılan davranıştan hem daha okunur hem daha az gürültülü.
app.UseSerilogRequestLogging();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Kültür çözümlemesi, kültüre bağlı hiçbir şey çalışmadan önce yapılmalı.
app.UseRequestLocalization();

app.UseRouting();

// Oturum ara katmanı yönlendirmeden sonra, controller'lardan önce çalışmalı:
// Device Session'a ilk erişim bir controller eyleminden gelir.
app.UseSession();

app.UseAuthorization();

app.MapHealthChecks("/health");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
