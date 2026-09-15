# Redis opsiyonel bağımlılık olarak koşullu kaydedilir

Visitor Session deposu ve Bus Location önbelleği için Redis kullanılır, ancak **zorunlu bir bağımlılık değildir**. Yapılandırmada bir Redis connection string varsa `AddStackExchangeRedisCache`, yoksa `AddDistributedMemoryCache` kaydedilir. Uygulama kodu yalnızca `IDistributedCache` arayüzünü tanıdığı için iki durumda da değişmez.

Gerekçe iki yönlüdür. Redis'in lehine olan argüman önbellek değil oturumdur: `AddDistributedMemoryCache`, adına rağmen tek process'in belleğidir, dolayısıyla uygulama yatay ölçeklendiğinde kullanıcının Device Session'ı instance'lar arasında kaybolur ve her istek yeni bir `GetSession` çağrısı üretir. Şartname açıkça "ölçeklenebilir" dediği için bu, iddia edilen bir özelliğin fiilen karşılanmasıdır. 20 lokasyon kaydını (~15 KB) önbelleklemek tek başına Redis'i haklı çıkarmazdı.

Redis'i zorunlu kılmamanın gerekçesi ise değerlendirme sürecidir: bu bir işe alım ödevi ve değerlendiricinin projeyi klonlayıp hiçbir kurulum yapmadan çalıştırabilmesi gerekiyor. Redis zorunlu olsaydı, `dotnet run` veya F5 ile açan biri uygulamayı hiç görmeden bir bağlantı hatasıyla karşılaşırdı — ve insanların varsayılan davranışı `docker compose up` değil, IDE'den çalıştırmaktır. Aynı gerekçe ApiClientToken'ın `appsettings.json` içinde tutulması kararının da dayanağıdır.

## Consequences

- `dotnet run` ve `docker compose up` yollarının **ikisi de** çalışır durumda tutulmalı; birinin bozulması sessizce fark edilmez.
- `docker-compose.yml` dosyasında Redis portu host'a publish **edilmez**: Device Session bir kimlik bilgisidir.
