# Uygulama Planı

Bu plan, obilet.com Sr. Full Stack Developer ödevi için yapılan tasarım görüşmesinin (28 karar) çıktısıdır. Kararların gerekçeleri `docs/adr/` altındadır, alan terimleri `CONTEXT.md` içindedir.

## Bağlam

Kullanıcı Origin, Destination ve Departure Date seçer; uygulama obilet business API'sinden uygun Journey listesini çeker ve kalkış saatine göre sıralı gösterir. İki sayfa: arama formu ve sefer listesi. Tüm API çağrıları backend'de yapılır; tarayıcı yalnızca kendi backend'imizle konuşur.

Plan, canlı API üzerinde doğrulanmış yedi bulgu üzerine kuruludur. Bunlar resmî dokümanla veya şartnameyle çelişiyor ve mimariyi doğrudan şekillendiriyor:

| # | Bulgu |
|---|---|
| 1 | Dokümandaki `GetSession` gövdesi (`type:7` + `application`) hata veriyor. Yalnızca Postman gövdesi (`type:1` + `connection.port` + `browser`) çalışıyor. |
| 2 | Hatalar **HTTP 200** ile dönüyor; başarısızlık yalnızca gövdedeki `status` alanından anlaşılıyor. Sunucu kendi stack trace'ini sızdırıyor. Geçersiz Device Session ise HTTP 400 + `DeviceSessionError` veriyor. |
| 3 | `GetBusLocations` hiçbir zaman tüm lokasyonları döndürmüyor: `data:null` → tam 20 kayıt. Arama da 20 ile sınırlı. |
| 4 | Arama asla boş dönmüyor; anlamsız girdide en popüler 10 lokasyona düşüyor. Eşleştirme `keywords` üzerinden bulanık. |
| 5 | Seferler sırasız geliyor ve ertesi güne taşıyor: 425 sefer / 3,2 MB, 53'ü ertesi gün 03:00'a kadar. |
| 6 | API minimum tarih kuralını uygulamıyor (dünün tarihi `Success` + 150 sefer). Geçersiz lokasyon ID'si `InvalidLocation` değil, 0 sonuçlu `Success` veriyor. |
| 7 | `language` bir pazar seçici. `en-US` → Türk pazarı İngilizce; `en-GB` → Britanya lokasyonları; **`en-EN` süresiz askıda kalıyor**; tanınmayan locale'ler de askıda kalıyor. |

## Çözüm yapısı

```
Obilet.sln
├── src/
│   ├── Obilet.Web/             ASP.NET Core MVC (.NET 10), Controllers, Views, wwwroot, Resources
│   ├── Obilet.Application/     Servisler, view model'ler, arayüzler, doğrulama
│   └── Obilet.Infrastructure/  Tipli obilet API istemcisi, DTO'lar, önbellek, oturum
├── tests/
│   └── Obilet.Tests/           xUnit
├── docs/adr/                   Mimari karar kayıtları
├── CONTEXT.md                  Alan sözlüğü
├── global.json                 SDK sürümü sabitlenir
├── Dockerfile                  Multi-stage
└── docker-compose.yml          Uygulama + Redis
```

Bağımlılık yönü daima içe doğru: `Web → Application ← Infrastructure`. `Application` hiçbir HTTP tipine bağlı değildir.

## Yapılacaklar

### 1. İskelet ve yapılandırma

- `.NET 10` hedefli çözüm, `global.json` ile SDK sabitlenir.
- `ObiletApiOptions` (BaseUrl, ApiClientToken, Timeout) `appsettings.json`'dan options binding ile bağlanır.
- `IHttpClientFactory` ile tipli `HttpClient`; **`Timeout` ~15s** (bulgu 7 nedeniyle zorunlu).

### 2. API istemcisi — `Obilet.Infrastructure`

- `IObiletApiClient`: `GetSessionAsync`, `GetBusLocationsAsync(query)`, `GetBusJourneysAsync(origin, destination, date)`.
- Ortak istek sarmalayıcı: `data` + `device-session` + `date` + `language`.
- **Yanıt yorumlama tek noktada**: `status != "Success"` ise tipli `ObiletApiException` fırlatılır (bulgu 2). Upstream `message` loglanır, asla render edilmez.
- `GetSession` gövdesi Postman şekliyle kurulur (bulgu 1); dokümandaki şeklin neden kullanılmadığı yorumla belirtilir.
- JSON sözleşmesi kebab-case; `JsonSerializerOptions` tek yerde tanımlanır.

### 3. Oturum — Device Session / Visitor Session

- `IDeviceSessionProvider.GetOrCreateAsync()`: Visitor Session içinde Device Session yoksa oluşturur, varsa döndürür.
- `DeviceSessionError` alındığında **bir kez** yeniden oluşturup isteği tekrarlar (bulgu 2).
- Depo `IDistributedCache`; Redis connection string varsa Redis, yoksa in-memory (ADR-0003).
- Device Session hiçbir view model'e, hiçbir JSON yanıtına, hiçbir cookie'ye girmez.

### 4. Lokalizasyon ve Market Locale

- `RequestLocalizationOptions`: yalnızca `tr-TR` ve `en-US`. Varsayılan `tr-TR`. Cookie provider önce, sonra `Accept-Language`.
- `IMarketLocaleResolver`: Display Culture → Market Locale beyaz listesi (`tr-TR`→`tr-TR`, `en-*`→`en-US`, diğer→`tr-TR`). API istemcisi `CultureInfo`'yu **asla** doğrudan geçirmez (ADR-0002).
- `.resx` + `IStringLocalizer` / `IViewLocalizer`; `tr` ve `en` **eksiksiz** doldurulur, görünür dil değiştirici.
- Nötr anahtarlar (`Search_OriginLabel`), Türkçe metin anahtar olarak kullanılmaz.

### 5. Lokasyonlar ve otomatik tamamlama

- `ILocationService.GetDefaultAsync()`: 20 kayıtlık liste, `IDistributedCache`'te **kültür başına** anahtarlanmış, 30 dk (ADR-0004).
- `SearchAsync(query)`: önbelleklenmez. En az 2 karakter.
- **Eşleşme doğrulaması** (bulgu 4): dönen kayıtların `name`/`keywords` alanında terim gerçekten eşleşmiyorsa sonuç boş sayılır. Karşılaştırma `tr-TR` kültürüyle, `İ/ı` katlaması doğru yapılır.
- `LocationsController` → `GET /api/locations/search?q=`; Tom Select'in remote yükleyicisi buraya bağlanır. Debounce 300ms.

### 6. Arama sayfası — Index

- Varsayılan Origin/Destination: API'nin döndürdüğü sıranın ilk iki kaydı (rank 1, 2 = İstanbul Avrupa, İstanbul Anadolu — doğrulandı, 31 seferi var).
- Varsayılan Departure Date: **yarın**; `Yarın` çipi seçili render edilir (ADR-0005).
- Takas butonu: dairesel, iki kartın dikiş yerinde, sadece değerleri yer değiştirir.
- `Bugün` / `Yarın` çipleri tarih alanını set eder ve seçili durumu günceller.
- **Doğrulama** (bulgu 6): `IValidatableObject` ile Origin ≠ Destination ve Date ≥ bugün; istemci tarafında ayna doğrulama. Hata mesajları `.resx`'ten.
- `localStorage` (`obilet.lastSearch`): son sorgu saklanır, dönüşte varsayılan olur. Eskimiş tarih **bugüne çekilir** ve kullanıcıya küçük bir not gösterilir. Tüm erişim `try/catch` içinde.

### 7. Sefer listesi — Journey Index

- Route: `/seferler/{originId}-{destinationId}/{date}` (obilet'in dokümante ettiği kalıp).
- Doğrudan URL erişimi mümkün olduğu için **aynı doğrulama sunucuda tekrar uygulanır**.
- **Sıralama: tam `DateTime` artan** (bulgu 5). `OrderBy(j => j.Departure.TimeOfDay)` bir hatadır ve bir test bunu korur. Ertesi gün grubu görsel ayırıcıyla belirtilir.
- İnce projeksiyon: ~100 alandan ~12 alanlık `JourneyViewModel` (3,2 MB → onlarca KB). Tüm satırlar render edilir, sayfalama yok.
- Satır içeriği: `KALKIŞ` / `VARIŞ` saatleri, terminal güzergâhı, **Partner adı + logosu**, fiyat.
- Fiyat: `internet-price` belirgin, `original-price` farklıysa üstü çizili. `tr-TR` biçimiyle `499,00 TL`.
- Özellik metni gerekirse **`features[].name`** kullanılır, `journey.features[]` değil (ikincisi `en-US`'te de Türkçe kalıyor).
- Partner logosu: `https://s3.eu-central-1.amazonaws.com/static.obilet.com/images/partner/{partner-id}-sm.png`, `onerror` ile yedek.
- Boş sonuç ve `InvalidRoute` → hata sayfası değil, dostane "sefer bulunamadı" durumu.
- Başlık: geri oku, `Origin - Destination`, alt satırda tarih.

### 8. Arayüz

- Bootstrap 5 + Tom Select (jQuery yok). Mobil-öncelikli responsive (ADR-0005).
- Palet XD şartnamesinden: `#2F4EB4` birincil mavi, `#D23B38` fiyat kırmızısı, `#192289` saatler, `#F8F8F8` / `#F3F3F3` zeminler.
- Global exception filter → dostane hata görünümü; upstream detay asla gösterilmez.

### 9. Testler — `Obilet.Tests`

1. Sıralama: ertesi güne taşan küme doğru sıralanıyor (bulgu 5 tuzağı).
2. Doğrulama: Origin = Destination reddediliyor; geçmiş tarih reddediliyor.
3. Market Locale eşlemesi: `en-GB` → `en-US`, tanınmayan → `tr-TR`, **`en-EN` hiçbir zaman üretilmiyor**.
4. Tarih çekme: eskimiş `localStorage` tarihi bugüne çekiliyor.
5. Eşleşme doğrulaması: alakasız sonuç kümesi boş sayılıyor; `İ/ı` katlaması doğru.
6. `HttpMessageHandler` mock'u ile: HTTP 200 + `status != Success` → `ObiletApiException`; `DeviceSessionError` → bir kez yeniden deneme.

### 10. Docker ve dokümantasyon

- Multi-stage `Dockerfile`; `docker-compose.yml` uygulama + Redis, healthcheck + `depends_on`, **Redis portu publish edilmez**.
- `README.md`: kurulum (`dotnet run` **ve** `docker compose up`), mimari özet, **keşfedilen API tutarsızlıkları bölümü** (yukarıdaki 7 bulgu), bilinçli kapsam dışı bırakılanlar (sayfalama, entegrasyon testleri).
- Anlamlı commit geçmişi; tek "initial commit" ile teslim edilmez.

## Doğrulama

1. `dotnet build` ve `dotnet test` temiz geçer.
2. **Kurulumsuz çalıştırma kabul kriteri**: temiz bir klonda, Redis olmadan `dotnet run` → uygulama açılır ve arama çalışır.
3. `docker compose up` → uygulama Redis ile açılır; Redis'in kullanıldığı loglardan doğrulanır.
4. Index: varsayılanlar İstanbul Avrupa / İstanbul Anadolu / yarın. Takas, `Bugün`/`Yarın`, metin arama çalışır.
5. Aynı lokasyon ve geçmiş tarih hata mesajı üretir; doğrudan URL ile de engellenir.
6. `/seferler/349-356/{yarın}` → 400+ sefer, kalkış saatine göre artan, ertesi gün seferleri sonda.
7. Dil değiştirici `en-US`'e geçer; lokasyon adları `Istanbul Europe` olur, arayüz İngilizceye döner.
8. Arama alanına `xqjz` → "sonuç bulunamadı" (popüler lokasyonlar **değil**).
9. Yeniden ziyarette son sorgu geri yüklenir; eskimiş tarih bugüne çekilir.
