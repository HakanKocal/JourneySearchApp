# Uygulama Planı ve Teslim Kaydı

Bu belge, obilet.com Sr. Full Stack Developer ödevi için yapılan tasarım görüşmesinin (28 karar) çıktısı olarak yazıldı ve uygulama tamamlandıktan sonra **fiilen teslim edilen hâle** göre güncellendi. Planlanandan sapılan yerler ayrıca işaretlidir.

Kararların gerekçeleri `docs/adr/` altındadır, alan terimleri `CONTEXT.md` içindedir. Kullanıcıya dönük özet `README.md`'dedir.

**Durum:** 10 iş kaleminin tamamı tamamlandı ve teslim öncesi kod incelemesindeki dört bulgu düzeltildi. 166 .NET testi ve 18 JavaScript testi geçiyor; derleme 0 uyarı / 0 hata.

## Bağlam

Kullanıcı Origin, Destination ve Departure Date seçer; uygulama obilet business API'sinden uygun Journey listesini çeker ve kalkış saatine göre sıralı gösterir. İki sayfa: arama formu ve sefer listesi. Tüm API çağrıları backend'de yapılır; tarayıcı yalnızca kendi backend'imizle konuşur.

Plan, canlı API üzerinde doğrulanmış bulgular üzerine kuruludur. Bunlar resmî dokümanla veya şartnameyle çelişiyor ve mimariyi doğrudan şekillendirdi. İlk yedisi kod yazılmadan önce, sekizincisi uygulama sırasında bulundu:

| # | Bulgu |
|---|---|
| 1 | Dokümandaki `GetSession` gövdesi (`type:7` + `application`) hata veriyor. Yalnızca Postman gövdesi (`type:1` + `connection.port` + `browser`) çalışıyor. |
| 2 | Hatalar **HTTP 200** ile dönüyor; başarısızlık yalnızca gövdedeki `status` alanından anlaşılıyor. Sunucu kendi stack trace'ini sızdırıyor. Geçersiz Device Session ise HTTP 400 + `DeviceSessionError` veriyor. |
| 3 | `GetBusLocations` hiçbir zaman tüm lokasyonları döndürmüyor: `data:null` → tam 20 kayıt. Arama da 20 ile sınırlı. |
| 4 | Arama asla boş dönmüyor; anlamsız girdide en popüler 10 lokasyona düşüyor. Eşleştirme `keywords` üzerinden bulanık. |
| 5 | Seferler sırasız geliyor ve ertesi güne taşıyor: 427 sefer / 3,2 MB, bir kısmı ertesi gün 03:00'a kadar. |
| 6 | API minimum tarih kuralını uygulamıyor (dünün tarihi `Success` + 150 sefer). Geçersiz lokasyon ID'si `InvalidLocation` değil, 0 sonuçlu `Success` veriyor. |
| 7 | `language` bir pazar seçici. `en-US` → Türk pazarı İngilizce; `en-GB` → Britanya lokasyonları; **`en-EN` süresiz askıda kalıyor**; tanınmayan locale'ler de askıda kalıyor. |
| 8 | API bir CDN arkasında ve `GetSession` çağrısı **hız sınırlı**. Sınır aşıldığında `HTTP 429` ve `Retry-After: 3556` (~1 saat) dönüyor; gövde JSON değil, düz metin `error code: 1015`. Bu, ziyaretçi başına oturumun yeniden kullanılmasını bir optimizasyon değil **zorunluluk** yapıyor. |

## Çözüm yapısı

```
Obilet.slnx
├── src/
│   ├── Obilet.Application/     Alan modelleri, servisler, arayüzler, kurallar
│   │   ├── Abstractions/       IObiletApiClient, IDeviceSessionAccessor,
│   │   │                       IObiletCallExecutor, IVisitorSessionStore
│   │   ├── Caching/            ILocationCache
│   │   ├── Exceptions/         ObiletApiException
│   │   ├── Journeys/           JourneyService, JourneyOrdering, SearchQueryValidator
│   │   ├── Localization/       MarketLocale, MarketLocaleResolver
│   │   ├── Locations/          LocationService, LocationRelevance, TurkishSearchText
│   │   ├── Models/             DeviceSession, BusLocation, Journey
│   │   └── Sessions/           DeviceSessionAccessor, ObiletCallExecutor
│   ├── Obilet.Infrastructure/  Tipli API istemcisi, sözleşmeler, önbellek
│   │   ├── Caching/            DistributedLocationCache
│   │   └── Obilet/             ObiletApiClient, ObiletApiOptions, ObiletJson, Contracts
│   └── Obilet.Web/             ASP.NET Core MVC (.NET 10)
│       ├── Controllers/        Home, Journey, Locations, Culture
│       ├── Filters/            ObiletApiExceptionFilter
│       ├── Formatting/         MoneyFormatter, PartnerLogo
│       ├── Resources/          SharedResource.resx + .en-US.resx
│       ├── Sessions/           HttpVisitorSessionStore
│       ├── Validation/         SearchQueryErrorMessages
│       └── wwwroot/            obilet.css, search-form.js
├── tests/
│   ├── Obilet.Tests/           xUnit — 133 test, 15 dosya
│   └── js/                     Node test runner — 18 test
├── docs/adr/                   5 mimari karar kaydı
├── CONTEXT.md                  Alan sözlüğü (16 terim)
├── NuGet.config                Depoya özel paket kaynağı
├── global.json                 .NET 10 sabitlemesi
├── Dockerfile                  Çok aşamalı
└── docker-compose.yml          Uygulama + Redis
```

Bağımlılık yönü daima içe doğru: `Web → Application ← Infrastructure`. `Application` hiçbir HTTP tipine bağlı değildir.

## Teslim edilenler

### 1. İskelet ve yapılandırma

- `.NET 10` hedefli çözüm; `global.json` ile sabitlenmiş.
- `ObiletApiOptions` `appsettings.json`'dan options binding ile bağlanır, `ValidateOnStart` ile açılışta doğrulanır.
- `IHttpClientFactory` ile tipli `HttpClient`; **`Timeout` 15s** (bulgu 7 nedeniyle zorunlu).

> **Sapma —** `global.json` tam yama sürümünü (`10.0.401`) çiviliyordu. Docker imajı farklı bir feature band taşıdığında `rollForward: latestFeature` geriye eşleşmediği için derleme kırıldı. `10.0.100` + `latestFeature` olarak gevşetildi; ana sürüm sabitlemesi korunuyor.
>
> **Eklendi —** depoya özel `NuGet.config`. Makinede global olarak tanımlı, kimlik doğrulaması gerektiren bir besleme restore işlemini 401 ile durduruyordu. Projeyi klonlayan birinin bizim kimlik bilgilerimize sahip olmasını bekleyemeyiz.

### 2. API istemcisi — `Obilet.Infrastructure`

- `IObiletApiClient`: `CreateSessionAsync`, `GetBusLocationsAsync(query)`, `GetBusJourneysAsync(...)`.
- Ortak istek sarmalayıcı: `data` + `device-session` + `date` + `language`.
- **Yanıt yorumlama tek noktada**: `status != "Success"` ise tipli `ObiletApiException` fırlatılır (bulgu 2). Upstream `message` loglanır, asla render edilmez.
- `GetSession` gövdesi Postman şekliyle kurulur (bulgu 1); bir test dokümandaki şekle dönülmesini engeller.
- JSON sözleşmesi kebab-case; `JsonSerializerOptions` tek yerde.

> **Eklendi —** `NumberHandling = AllowReadingFromString`. Doküman bazı sayısal alanları string olarak belgeliyor, örnek yanıtlarda sayı geliyor; iki biçimi de kabul etmek sözleşmedeki tutarsızlığın uygulamayı düşürmesini engelliyor.
>
> **Eklendi —** `HttpStatusCode` ve `RetryAfter` istisnaya taşınıyor (bulgu 8). Hız sınırı tespiti gövdeye tek başına güvenmiyor.

### 3. Oturum — Device Session / Visitor Session

- `IDeviceSessionAccessor.GetOrCreateAsync()`: Visitor Session içinde Device Session yoksa oluşturur, varsa yeniden kullanır.
- `IObiletCallExecutor`: `DeviceSessionError` alındığında oturumu yenileyip isteği **bir kez** tekrarlar. Servisler bu kalıbı hiç görmez.
- Depo `IDistributedCache`; Redis varsa Redis, yoksa süreç içi bellek (ADR-0003).
- Device Session hiçbir view model'e, JSON yanıtına veya cookie'ye girmez — canlı olarak doğrulandı.

### 4. Lokalizasyon ve Market Locale

- `RequestLocalizationOptions`: yalnızca `tr-TR` ve `en-US`, desteklenen kültüreler `MarketLocale.Supported`'dan türetilir.
- `IMarketLocaleResolver`: Display Culture → Market Locale beyaz listesi. API istemcisi `CultureInfo`'yu **asla** doğrudan geçirmez; beyaz liste **iki katmanda** birden uygulanır (ADR-0002).
- `.resx` + `IStringLocalizer` / `IViewLocalizer`; iki dil de eksiksiz, nötr anahtarlar.
- `CultureController` dil tercihini çereze yazar; gelen değer beyaz listeden geçer, dönüş adresi yerel olarak doğrulanır ve adresteki `culture` parametresi ayıklanır.

> **Sapma —** dil değiştirici alt kısımda duruyor, başlıkta değil. Şartnamedeki üst çubuk boş ve sefer sayfası kendi başlığını getirdiği için her iki sayfada görünen ve tasarımla çakışmayan tek yer orası.

### 5. Lokasyonlar ve otomatik tamamlama

- `GetDefaultAsync()`: 20 kayıtlık liste, `IDistributedCache`'te **Market Locale başına** anahtarlı, 30 dk (ADR-0004). Boş sonuç önbelleğe alınmaz.
- `SearchAsync(query)`: önbelleklenmez, en az 2 karakter.
- **Eşleşme doğrulaması** (bulgu 4): `LocationRelevance` dönen kayıtları `name` ve `keywords` üzerinden süzer; hiçbiri ilişkili değilse sonuç boş sayılır.
- `TurkishSearchText`: `i` harfinin dört varyantı tek değere katlanır, Türkçe harfler ASCII karşılıklarına indirgenir.
- `LocationsController` → `GET /api/locations/search?q=`; yanıt yalnızca `id` ve `name` taşır.
- Tom Select remote yükleyicisi buraya bağlı, 300ms gecikme.

> **Sapma —** Türkçe katlama kuralları **iki yerde**: `TurkishSearchText.cs` ve `search-form.js`. Tek yerde tutmak mümkün değil, çünkü sunucu API sonucunu, istemci ise sayfayla birlikte gelen varsayılan listeyi süzüyor. Bu bir tarayıcı testinde bulundu: yerel süzme kapatıldığında "ankara" araması 20 alakasız şehri de listeliyordu.
>
> **Eklendi —** istemci, sunucunun döndürdüğü kimlikleri ayrı bir kümede izliyor. Sunucu eşleşmelerinin bir kısmı API'nin anahtar kelime alanından geliyor ve istemci o alanı görmüyor: "esenler" araması "İstanbul Avrupa" döndürüyor ve yalnızca ada bakan bir süzgeç bu doğru sonucu elerdi.

### 6. Arama sayfası

- Varsayılan Origin/Destination: API sıralamasının ilk iki kaydı (349 İstanbul Avrupa, 350 İstanbul Anadolu).
- Varsayılan Departure Date **yarın**, `Yarın` çipi seçili.
- Takas butonu dairesel, iki kartın dikişinde; karşı alanda olmayan seçeneği önce ekler.
- `Bugün` / `Yarın` çipleri tarihi set eder ve seçili durumu yansıtır.
- `localStorage` (`obilet.lastSearch`): son sorgu saklanır ve dönüşte geri yüklenir. Eskimiş tarih bugüne çekilir ve kullanıcıya bildirilir. Tüm erişim `try/catch` içinde.

> **Eklendi —** kayıtta lokasyonun kimliğinin yanında **adı da** saklanıyor. Kullanıcı metin aramasıyla varsayılan 20 kaydın dışında bir yer seçebiliyor; yalnızca kimlik saklamak geri yüklerken boş bir seçim üretiyordu.
>
> **Sapma —** istemci tarafı JavaScript view içine gömülü değil, `wwwroot/js/search-form.js` dosyasında. Tarayıcı önbelleğine giriyor, katlama kodu tek yere toplanıyor ve saf mantık Node ile test edilebiliyor.

### 7. Sefer listesi

- Route: `/seferler/{originId}-{destinationId}/{date}` (obilet'in dokümante ettiği kalıp). Biçimi bozuk tarih arama formuna yönlendirilir.
- **Sıralama: tam `DateTime` artan** (bulgu 5). `OrderBy(j => j.Departure.TimeOfDay)` bir hatadır; bir test iki sonucu karşılaştırarak bunu korur. Eşit kalkışlarda kimliğe göre kararlı sıra.
- Ertesi güne taşan grup görsel bir ayırıcıyla belirtilir.
- İnce projeksiyon: ~100 alandan 14 alanlık `Journey` modeli. Sayfa 3,2 MB yerine ~230 KB. Tüm satırlar render edilir, sayfalama yok.
- Satır içeriği: `KALKIŞ` / `VARIŞ` saatleri, süre, terminal güzergâhı, Partner adı + logosu, fiyat.
- Fiyat: `internet-price` belirgin, `original-price` farklıysa üstü çizili.
- Boş sonuç ve `InvalidRoute` → hata sayfası değil, bilgilendirici boş durum.

> **Eklendi — `MoneyFormatter`.** Fiyatlar başta `ToString("C2")` ile biçimlendirilmişti ve ambient kültürün para birimi sembolünü kullandığı için İngilizce arayüzde Türk Lirası tutarları **`$900.00`** olarak görünüyordu. Artık sayı biçimi kültürden, para birimi API'nin bildirdiği koddan geliyor.
>
> **Kapsam dışı —** özellik ikonları, koltuk sayısı ve otobüs tipi gösterilmiyor (tasarım kararı; şartname de göstermiyor). Dolayısıyla "özellik metni `features[].name`'den okunur" notu bu uygulamada uygulanamaz durumda; ileride özellikler gösterilmek istenirse geçerli olacak.

### 8. Doğrulama

- `SearchQueryValidator`: Origin ≠ Destination ve Date ≥ bugün.
- Hem form POST'unda hem sefer sayfasının adresinde uygulanır.
- İstemci tarafında ayna doğrulama ve tarih alanında `min` özniteliği.
- Hatalar sefer sayfasından forma **kural adı** olarak taşınır, metin olarak değil; kullanıcı dil değiştirse bile mesaj doğru dilde üretilir.

> **Sapma —** doğrulama `IValidatableObject` ile view model içine değil, paylaşılan bir kural sınıfına konuldu. Sefer sayfası route parametreleriyle geliyor ve view model'e hiç bağlanmıyor; `IValidatableObject` o yolu **görmezdi**.

### 9. Arayüz ve tasarım

- Bootstrap 5 + Tom Select. jQuery yok.
- Palet XD şartnamesinden CSS değişkenleri olarak; ölçülen değerler şartnameyle birebir (`#2F4EB4` / 40px üst çubuk, 208px buton, `#5D686E` seçili çip).
- Mobil-öncelikli responsive (ADR-0005). Layout devredilebilir bir `Header` bölümü sunar.
- Şablonla gelen `site.css`, `site.js`, `_Layout.cshtml.css`, `_ValidationScriptsPartial.cshtml` ve jQuery kaldırıldı.

> **Eklendi —** erişilebilirlik düzeltmesi. Şartnamede görünür başlık olmadığı için sayfa başlıksız kalıyordu; arama sayfasına gizli bir `h1`, sefer sayfasında güzergâh adı `h1` olarak işaretlendi.
>
> **Düzeltildi —** 320px'te güzergâh ve firma adı aynı satırı paylaşınca uzun firma adlarının yanındaki güzergâh dört satıra bölünüyor ve kart diğerlerinin iki katı yükseliyordu. Dar ekranda güzergâh kendi satırını alıyor.

### 10. Hata yönetimi

`ObiletApiExceptionFilter` tek bir yerde sınıflandırma yapar:

| Durum | HTTP | Kullanıcıya |
|---|---|---|
| Hız sınırı (429) | 503 | "Şu anda çok fazla istek var" |
| Diğer API hataları | 502 | "Bir şeyler ters gitti" |
| Timeout | 504 | "Bir şeyler ters gitti" |
| Ağ hatası | 502 | "Bir şeyler ters gitti" |

JSON uç noktasına HTML hata sayfası gönderilmez. Upstream detay ve ilişkilendirme kimliği loglanır, kullanıcı yalnızca bir referans kimliği görür.

> **Eklendi —** ağ hatası ve timeout sınıflandırması. Başta yalnızca `ObiletApiException` yakalanıyordu; erişilemeyen bir API `HttpRequestException` fırlattığı için filtreye uğramıyor ve 500 dönüyordu. Timeout bu uygulamada beklenen bir senaryo (bulgu 7).

### 11. Önbellek, Docker ve dokümantasyon

- `IDistributedCache`; Redis bağlantı dizesi varsa Redis, yoksa süreç içi bellek. Hangisinin seçildiği açılışta loglanır.
- Çok aşamalı `Dockerfile`; imaj 245 MB, kurulu SDK sayısı 0, root olmayan kullanıcı.
- `docker-compose.yml`: uygulama + Redis, healthcheck + `depends_on: service_healthy`, **Redis portu publish edilmez**.
- `README.md`: iki çalıştırma yolu, sekiz API bulgusu, mimari özet, bilinçli kapsam dışı bırakılanlar, bilinen sınırlar.
- 13 commit; tek "initial commit" yok.

> **Eklendi — `/health` uç noktası.** İlk compose healthcheck'i ana sayfayı yokluyordu; o sayfa obilet API'sini çağırdığı için API yavaşladığında konteyner gereksizce sağlıksız işaretlenip yeniden başlatılıyordu. Sağlık kontrolü bilinçli olarak ne API'yi ne Redis'i yokluyor.
>
> **Düzeltildi —** healthcheck `wget` kullanıyordu ama `aspnet` temel imajı ne `wget` ne `curl` içeriyor; uygulama tamamen sağlıklıyken konteyner `unhealthy` işaretleniyordu. Runtime aşamasına `curl` kuruldu.
>
> **Düzeltildi —** lokasyon önbelleği anahtarı `obilet:obilet:...` şeklinde çift önek taşıyordu; Redis kaydı zaten bir `InstanceName` öneki uyguluyor.

## Testler

**133 .NET testi** (15 dosya) ve **18 JavaScript testi**. JavaScript testleri `node --test tests/js/` ile ayrı çalışır; test edilen mantık `localStorage` ve tarayıcı davranışı üzerine kurulu olduğu için ancak bir JavaScript çalıştırıcısı doğrulayabilir.

Testler tam olarak bu projenin riskinin yaşadığı yerleri kapsıyor:

| Alan | Neyi koruyor |
|---|---|
| `JourneyOrderingTests` | Ertesi güne taşan kümenin doğru sıralanması; naif sıralamayla karşılaştırma |
| `MarketLocaleTests` | Beyaz liste; `en-EN` hiçbir girdiden üretilemez |
| `ObiletApiClientTests` | HTTP 200 içindeki hata; dokümandaki gövdeye dönülmemesi |
| `RateLimitHandlingTests` | 429 sınıflandırması; JSON olmayan gövdenin çökmemesi |
| `SearchQueryValidatorTests` | İki kural; bugünün geçerli olduğu sınır |
| `LocationRelevanceTests` | Anlamsız terimde boş sonuç; anahtar kelime eşleşmesinin korunması |
| `TurkishSearchTextTests` | `i` varyantlarının katlanması; ASCII karşılıkları |
| `MoneyFormatterTests` | Para biriminin kültürle değişmemesi |
| `SharedResourceParityTests` | İki dilin anahtar paritesi, boş değer yokluğu, yer tutucu eşitliği |
| `DeviceSessionAccessorTests` | Ziyaretçi başına oturum; yeniden kullanım |
| `ObiletCallExecutorTests` | Bir kez yeniden deneme; oturumla ilgisiz hataların tekrarlanmaması |
| `DistributedLocationCacheTests` | Kültür başına anahtarlama; önbellek düştüğünde çalışmaya devam |
| `search-form.test.mjs` | Tarih çekme; depolama erişimi engelliyken çökmeme |

## Doğrulama sonuçları

Tümü canlı API ile çalıştırılarak doğrulandı:

| # | Kontrol | Sonuç |
|---|---|---|
| 1 | `dotnet build` / `dotnet test` | 0 uyarı, 0 hata, 133/133 |
| 2 | Redis olmadan `dotnet run` | Açılış logu "süreç içi bellek", arama uçtan uca çalışıyor |
| 3 | `docker compose up` | Redis `healthy` → web `healthy`; `dbsize` 0→2 |
| 4 | Varsayılanlar | İstanbul Avrupa / İstanbul Anadolu / yarın, `Yarın` çipi seçili |
| 5 | Aynı lokasyon ve geçmiş tarih | Formda hata; adresle de 302 ile engelleniyor |
| 6 | `/seferler/349-356/{yarın}` | 427 sefer, artan sıralı, ertesi gün grubu sonda ve ayırıcılı |
| 7 | Dil değiştirici | `Istanbul Europe – Ankara`, `16 September, Wednesday`, `900.00 ₺` |
| 8 | Arama `xqjz` | "Sonuç bulunamadı." — popüler lokasyonlar değil |
| 9 | Yeniden ziyaret | Son sorgu geri yüklendi; eskimiş tarih bugüne çekildi ve bildirildi |
| 10 | Oturum yeniden kullanımı | Aynı ziyaretçi 3 istek → 1 oturum; farklı ziyaretçi → 1 yeni oturum |
| 11 | Erişilemeyen API | HTML 502 + dostane sayfa, JSON 502 + `application/json`, sızıntı taraması sıfır |
| 12 | 320px | Kartlarda taşma yok; takas butonu dikişte |

## Kod incelemesi düzeltmeleri

Teslim öncesi incelemede dört bulgu çıktı; hepsi kendi yazdığım koddaydı ve düzeltildi.

| # | Bulgu | Düzeltme |
|---|---|---|
| 1 | Tarih hesapları sunucunun yerel saatini kullanıyordu; konteyner UTC, pazar UTC+3 | `IMarketClock` / `MarketClock`; controller'lar ve API istemcisi buna bağlandı |
| 2a | `h:mm` gün bileşenini düşürüyordu (25s30d → `1:30`) | `DurationFormatter`, toplam saat üzerinden |
| 2b | `25:30:00` biçimi tüm yanıtı düşürüyordu | `TolerantTimeSpanConverter`; ayrıştırılamayan süre `null` |
| 3 | Sunucu sonuç kimlikleri terim değiştikten sonra da eşleşme sayılıyordu | Kimlikler geldikleri terimle birlikte tutuluyor |
| 4 | İstemci katlama haritası `Í í Î î` atlıyordu | Eklendi; JS testi dokuz varyantı kontrol ediyor |

**Bulgu 1'in önemi:** her gece 21:00–00:00 (UTC) arasında sunucunun "bugün"ü kullanıcının dünü oluyordu. O pencerede `Yarın` varsayılanı bugünü gösteriyor, `Bugün` çipi dünü arıyor ve şartnamenin "minimum geçerli tarih bugündür" kuralı doğrulamadan geçiyordu (`dün < dün` yanlış olduğu için).

**Düzeltme sırasında bulunan ek hata:** `25:30:00` için eklediğim ilk dönüştürücü `TimeSpan.TryParse`'a güveniyordu, ancak o `48:00:00` değerini başarısız saymıyor — `d:hh:mm` sanıp 48 **gün** olarak ayrıştırıyor. İstisna fırlatmasından daha kötü bir sonuç: sessizce yanlış bir süre. Bileşenler artık elle okunuyor. Bunu yazdığım bir test ortaya çıkardı.

### Bulgu 1'in canlı doğrulaması

Kasten uzak bir saat dilimine alınmış bir konteynerde çalıştırıldı:

```
konteynerin yerel tarihi : 2026-09-16 08:32 NZST
pazarın tarihi (TR)      : 2026-09-15 23:32
uygulamanın "bugün"ü     : min="2026-09-15"    ← pazarın tarihi
varsayılan kalkış        : value="2026-09-16"  ← pazar açısından yarın
```

Düzeltme öncesi aynı konteyner `min="2026-09-16"` bildirecekti. Gece yarısı eşiğinin kendisi `MarketClockTests` içinde sahte bir saatle deterministik olarak kapsanıyor; canlı ortamda beklemeye bağlı olmayan bir doğrulama bu şekilde yapıldı.

### Doğrulanamayan üç şey

Bunlar bilinçli olarak eksik bırakıldı, gizlenmedi:

1. **Hız sınırı sayfası (503) canlı tetiklenmedi.** API'yi kasten sınıra sokmak bir saatlik engel üretirdi. Sınıflandırma ve JSON olmayan gövdenin uygulamayı düşürmemesi `RateLimitHandlingTests` ile kapsanıyor; görünüm farkı yalnızca gösterilen metinde.
2. **Gerçek 320px görüntü alanı elde edilemedi.** Tarayıcı penceresi maximize olduğu için yeniden boyutlandırma çağrısı `outerWidth`'i değiştirmedi. Mobil yerleşim, içerik kabı 320px'e sabitlenip mobil kurallar zorlanarak doğrulandı ve 427 kartın hiçbirinde taşma olmadığı ölçüldü. Gerçek bir cihazda yeniden akış kontrol edilmedi.
3. **Depolama erişiminin engellendiği durum tarayıcıda denenmedi.** Gizli sekme davranışı, hata fırlatan bir depolama taklidiyle Node testlerinde kapsanıyor.
