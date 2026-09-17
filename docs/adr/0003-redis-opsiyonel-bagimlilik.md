# Redis opsiyonel bağımlılık olarak koşullu kaydedilir

Visitor Session deposu ve Bus Location önbelleği için Redis kullanılır, ancak **zorunlu bir bağımlılık değildir**. Yapılandırmada bir Redis connection string varsa `AddStackExchangeRedisCache`, yoksa `AddDistributedMemoryCache` kaydedilir. Uygulama kodu yalnızca `IDistributedCache` arayüzünü tanıdığı için iki durumda da değişmez.

Gerekçe iki yönlüdür. Redis'in lehine olan argüman önbellek değil oturumdur: `AddDistributedMemoryCache`, adına rağmen tek process'in belleğidir, dolayısıyla uygulama yatay ölçeklendiğinde kullanıcının Device Session'ı instance'lar arasında kaybolur ve her istek yeni bir `GetSession` çağrısı üretir. Şartname açıkça "ölçeklenebilir" dediği için bu, iddia edilen bir özelliğin fiilen karşılanmasıdır. 20 lokasyon kaydını (~15 KB) önbelleklemek tek başına Redis'i haklı çıkarmazdı.

Redis'i zorunlu kılmamanın gerekçesi ise değerlendirme sürecidir: bu bir işe alım ödevi ve değerlendiricinin projeyi klonlayıp hiçbir kurulum yapmadan çalıştırabilmesi gerekiyor. Redis zorunlu olsaydı, `dotnet run` veya F5 ile açan biri uygulamayı hiç görmeden bir bağlantı hatasıyla karşılaşırdı — ve insanların varsayılan davranışı `docker compose up` değil, IDE'den çalıştırmaktır. Aynı gerekçe ApiClientToken'ın `appsettings.json` içinde tutulması kararının da dayanağıdır.

## Consequences

- `dotnet run` ve `docker compose up` yollarının **ikisi de** çalışır durumda tutulmalı; birinin bozulması sessizce fark edilmez.
- `docker-compose.yml` dosyasında Redis portu host'a publish **edilmez**: Device Session bir kimlik bilgisidir.

## Aynı kalıp günlüklemeye de uygulandı

Elasticsearch'e günlük gönderimi sonradan eklendiğinde bu belgedeki gerekçe birebir tekrarlandı: bağlantı dizesi yoksa yalnızca konsola yazılır, Elasticsearch erişilemezse günlük kanalı tamponlayıp çalışmaya devam eder. Uygulama iki durumda da açılır ve hizmet verir.

İki fark var. Birincisi, Elasticsearch portu host'a **açılıyor**; Redis'teki kısıtlamanın gerekçesi kimlik bilgisi barındırmasıydı ve günlükler kimlik bilgisi içermiyor. İkincisi, `web` servisi Elasticsearch'ü `depends_on` içine **almıyor**: uygulama ona bağımlı olmadığı gibi, oraya yazmak `docker compose up web redis` ile yığını atlamayı da imkânsız kılıyordu.

Günlükler süreç dışına çıktığı için ne yazıldığı ayrıca ele alındı: `Microsoft.Extensions.Http` ve `System.Net.Http.HttpClient` kategorileri uyarı seviyesine çekildi, çünkü o kategoriler düşük seviyelerde istek başlıklarını — dolayısıyla `Authorization` başlığını — yazabiliyor.
