using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Obilet.Application;
using Obilet.Application.Abstractions;
using Obilet.Application.Localization;
using Obilet.Infrastructure;
using Obilet.Web.Sessions;

var builder = WebApplication.CreateBuilder(args);

// --- Lokalizasyon -----------------------------------------------------------
// Kaynak dosyaları Resources klasöründe; nötr dosya Türkçe metinleri taşır.
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services
    .AddControllersWithViews()
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

// --- Visitor Session ---------------------------------------------------------
// Device Session sunucu tarafında, ziyaretçiye özel olarak saklanır. Depo
// şimdilik süreç içi bellektir; dağıtık önbelleğe geçiş ayrı bir adımın işi
// ve uygulama kodunu değiştirmeyecek şekilde tasarlandı (bkz. docs/adr/0003).
builder.Services.AddDistributedMemoryCache();
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

// --- Katmanlar --------------------------------------------------------------
builder.Services.AddObiletApplication();
builder.Services.AddObiletInfrastructure(builder.Configuration);

var app = builder.Build();

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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
