# Çok aşamalı derleme: SDK yalnızca derleme aşamasında kullanılır ve
# çalışma zamanı imajına sızmaz. Sonuç, yaklaşık 100 MB'lık bir ASP.NET
# Core runtime imajı yerine 800 MB'lık bir SDK imajı taşımamak.

# --- Derleme aşaması ---------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# NuGet.config önce kopyalanır: makinede tanımlı özel beslemeleri devre dışı
# bırakıp yalnızca nuget.org'u kullanır, böylece konteyner içinde kimlik
# doğrulaması gerektiren bir beslemeye takılmaz.
COPY NuGet.config global.json ./

# Proje dosyaları kaynak koddan önce kopyalanır. Böylece kaynak kodda yapılan
# bir değişiklik restore katmanını geçersiz kılmaz ve paketler yeniden
# indirilmez. Katman önbelleklemesinden faydalanmanın standart yolu.
COPY Obilet.slnx ./
COPY src/Obilet.Application/Obilet.Application.csproj src/Obilet.Application/
COPY src/Obilet.Infrastructure/Obilet.Infrastructure.csproj src/Obilet.Infrastructure/
COPY src/Obilet.Web/Obilet.Web.csproj src/Obilet.Web/
COPY tests/Obilet.Tests/Obilet.Tests.csproj tests/Obilet.Tests/

RUN dotnet restore src/Obilet.Web/Obilet.Web.csproj

COPY . .

RUN dotnet publish src/Obilet.Web/Obilet.Web.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

# --- Çalışma zamanı aşaması --------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish ./

# Konteyner içinde HTTPS yönlendirmesi yapılmaz: TLS sonlandırma, önünde
# duran ters proxy veya yük dengeleyicinin işidir. Konteynerin kendi
# sertifikasını yönetmesi gereksiz karmaşıklık olurdu.
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

# Root olmayan kullanıcı. Temel imajın tanımladığı kullanıcı kullanılır;
# uygulama hiçbir ayrıcalıklı işlem yapmıyor.
USER $APP_UID

ENTRYPOINT ["dotnet", "Obilet.Web.dll"]
