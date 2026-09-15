using Obilet.Application;
using Obilet.Application.Abstractions;
using Obilet.Infrastructure;
using Obilet.Web.Sessions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

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

app.UseRouting();

// Oturum ara katmanı yönlendirmeden sonra, controller'lardan önce çalışmalı:
// Device Session'a ilk erişim bir controller eyleminden gelir.
app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
