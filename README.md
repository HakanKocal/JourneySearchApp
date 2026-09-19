# obilet.com Sefer Arama

obilet.com business API'si üzerinden şehirler arası otobüs seferi arayan iki sayfalık bir ASP.NET Core MVC uygulaması. Kullanıcı bir kalkış noktası, bir varış noktası ve bir tarih seçer; uygulama o sorgu için uygun seferleri kalkış saatine göre sıralı olarak listeler.

Tüm API çağrıları uygulama backend'inde yapılır. Tarayıcı yalnızca uygulamanın kendi uç noktalarıyla konuşur; `ApiClientToken` ve oturum kimlikleri istemciye hiç ulaşmaz.

## Arayüz

Arayüz verilen iki masaüstü tasarımını izler.

**Ana sayfa** fotoğraf zeminli bir hero taşır: sol tarafta başlık ve tanıtım metni, üzerine binen beyaz bir arama kartı, hero'nun altında dört maddelik bir tanıtım şeridi. Arama kartında hızlı tarih çipleri (`Bugün` / `Yarın`), aranabilir kalkış ve varış alanları, aralarında bir takas düğmesi ve bir tarih alanı var.

**Sefer listesi** aynı hero'nun kısa hâlini, üzerine binen bir sorgu özeti kartını ve her seferi tek satırda gösteren yatay kartları taşır. Özet kartındaki tarih çipleri ve yön çevirme ikonu gerçek bağlantı: aynı güzergâhın başka gününe veya ters yönüne tek tıkla gidilir. Kartta kalkış, varış, süre, terminaller, olanaklar, firma ve fiyat yer alır.

Kurulum mobil-öncelikli: taban stiller dar ekranı tarif eder, medya sorguları genişlikte verilen masaüstü düzenine açar. Gerekçe ve kaydedilen ödünleşmeler `docs/adr/0007`'de.

---

## Çalıştırma

İki yol var ve **ikisi de ek kurulum gerektirmez**.

### 1. Doğrudan

```bash
dotnet run --project src/Obilet.Web
```

Redis yapılandırılmadığı için uygulama süreç içi bellek önbelleğiyle çalışır. Açılış logunda hangi önbellek sağlayıcısının seçildiği yazar.

### 2. Docker ile (Redis + günlük yığını dâhil)

```bash
docker compose up
```

| Adres | Ne |
|---|---|
| `http://localhost:8080` | Uygulama |
| `http://localhost:5601` | Kibana — günlükleri görüntülemek için |
| `http://localhost:9200` | Elasticsearch (doğrulama kolaylığı için açık) |

Redis portu host'a açılmaz. Günlükler `logs-obilet-web-default` veri akışına ECS biçiminde yazılır.

**Kibana'da görmek için:** `http://localhost:5601` → Discover → **obilet-web gunlukleri**. Data view `docker compose up` sırasında otomatik oluşturulur; Kibana kendiliğinden hiçbir data view ile gelmediği ve onsuz Discover boş göründüğü için tek seferlik bir kurulum servisi bunu yapıyor.

Veri akışının arka index'i `.ds-` önekiyle başlar ve Kibana'nın **Indices** listesinde varsayılan olarak gizlidir — **Data Streams** sekmesinden görünür.

### Ne loglanıyor?

| Kaynak | Ne |
|---|---|
| İstek özeti | Her istek için tek satır: yöntem, adres, durum kodu, süre |
| `DeviceSessionAccessor` | Yeni bir Device Session oluşturulduğunda (kimlik bilgisi **yazılmaz**) |
| `ObiletCallExecutor` | Oturum geçersizleşip yenilendiğinde |
| `ObiletApiClient` | API başarısız durum döndürdüğünde: uç nokta, durum, ilişkilendirme kimliği, kırpılmış upstream mesaj |
| `DistributedLocationCache` | Önbelleğe ulaşılamadığında |
| `ObiletApiExceptionFilter` | Kullanıcıya hata gösterildiğinde, referans kimliğiyle |
| Açılış | Hangi önbellek sağlayıcısı ve hangi günlük hedefleri seçildi |
| Çerçeve | Yalnızca uyarı ve hata seviyesinde |
| Sağlık yoklamaları | **Başarılı olduklarında yazılmaz**; başarısız olduklarında hata olarak yazılır |

İki gürültü kaynağı bilinçli olarak bastırıldı; ikisi de ölçümle bulundu.

**Çerçevenin bilgi seviyesindeki istek günlükleri.** Bastırılmadan önce 178 kaydın yalnızca **2'si** uygulama kodundan geliyordu; gerisi her istek için üç dört satır yazan çerçeve kategorileriydi ve `UseSerilogRequestLogging` ile aynı bilgiyi tekrarlıyordu. Bastırıldıktan sonra dört istek **9 kayıt** üretiyor.

**Başarılı sağlık yoklamaları.** Docker healthcheck konteynerin içinde her 15 saniyede bir `/health` çağırıyor ve bu istekler günlüklerin en kalabalık grubuydu: 255 kaydın **72'si**, dakikada dört kayıt — boşta duran bir uygulama günde yaklaşık **5.760 kayıt** üretiyordu. Artık başarılı yoklamalar yazılmıyor; yoklama başarısız olduğunda dönen 503 yanıtı hata olarak kaydedildiği için sessizlik sağlıklı olmanın işareti oluyor.

Günlük yığını yaklaşık **1,5 GB bellek** istiyor. Yalnızca uygulamayı ve Redis'i kaldırmak için:

```bash
docker compose up web redis
```

Bu durumda uygulama hâlâ Elasticsearch'e yazmayı dener, ulaşamaz ve günlükleri tamponda tutar — çalışmaya sorunsuz devam eder, günlükler konsolda kalır.

> **Gereksinim:** .NET 10 SDK (doğrudan çalıştırma için) veya Docker. Depoda kendi `NuGet.config` dosyası var; makinede tanımlı özel paket beslemeleri bu çözüm için devre dışı bırakılır, böylece kimlik doğrulaması gerektiren bir besleme restore işlemini durdurmaz.

### Testler

```bash
dotnet test                # 201 test
node --test tests/js/      # 26 test
```

JavaScript testleri .NET paketine dâhil değildir: test edilen mantık `localStorage` ve tarayıcı davranışı üzerine kurulu olduğu için ancak bir JavaScript çalıştırıcısı doğrulayabilir.

---

## Canlı API incelemesinde bulunan tutarsızlıklar

Uygulama yazılmadan önce API canlı olarak incelendi. **Sekiz bulgu** resmî dokümanla veya ödev şartnamesiyle çelişiyor ve her biri mimariyi doğrudan şekillendirdi. Bu bölüm, kodda neden bazı şeylerin "gereğinden fazla" göründüğünü açıklar.

### 1. Dokümandaki `GetSession` gövdesi çalışmıyor

Doküman `type: 7` ve bir `application` nesnesi belgeliyor. API bunu reddediyor:

> `Port can not be null for browsers. (Parameter 'Port')` — `Browser can not be null for browsers.`

Çalışan gövde örnek Postman koleksiyonundaki `type: 1` + `connection.port` + `browser` biçimidir. Bir test yanlışlıkla dokümandaki şekle dönülmesini engelliyor.

### 2. Hatalar HTTP 200 ile dönüyor

Başarısızlıklar çoğunlukla `200 OK` ile geliyor; başarı yalnızca yanıt gövdesindeki `status` alanından anlaşılıyor. HTTP durum koduna güvenen bir uygulama hatayı sessizce başarı sayardı. Bu yüzden durum yorumlaması tek bir yerde, API istemcisinin içinde yapılıyor.

API ayrıca başarısızlıkta **kendi sunucu tarafı yığın izini** `message` alanında döndürüyor. Bu metin loglanıyor, hiçbir koşulda ekrana gelmiyor.

### 3. `GetBusLocations` hiçbir zaman tüm lokasyonları döndürmüyor

`data: null` çağrısı tam **20** kayıt veriyor; arama sonuçları da 20 ile sınırlı. Şartname "tüm olası lokasyonlar" istiyor ama API bunu sağlayamıyor. 20 kayıt başlangıç listesi kabul edildi, geri kalanına sunucu taraflı metin aramasıyla erişiliyor. Bkz. `docs/adr/0004`.

### 4. Arama asla boş dönmüyor

Anlamsız bir terim boş liste değil, **en popüler lokasyonları** döndürüyor: `xqjz` araması İstanbul ve Ankara veriyor. Sonucu olduğu gibi göstermek, aranan şey bulunmadığı hâlde bulunmuş gibi bir liste sunmak olurdu. Bu yüzden dönen kayıtlar terimle karşılaştırılıyor ve hiçbiri ilişkili değilse sonuç boş sayılıyor.

### 5. Seferler sırasız geliyor ve ertesi güne taşıyor

İstanbul–Ankara sorgusu **427 sefer / 3,2 MB** döndürüyor, sefer başına yüzden fazla alanla. Liste sıralı değil ve istenen günle de sınırlı değil: seferlerin bir kısmı gece yarısını aşıp ertesi sabah 03:00'a kadar uzanıyor.

Bu nedenle sıralama **tam tarih-saat** değerine göre yapılıyor. `OrderBy(j => j.Departure.TimeOfDay)` bu veride yanlıştır — ertesi günün 00:05 otobüsünü aynı günün 23:50 otobüsünün önüne atar. Bir test iki sonucu karşılaştırarak bu tuzağı koruyor.

Ayrıca sunucu tarafında ince bir projeksiyon yapılıyor: sayfa 3,2 MB yerine ~230 KB iniyor.

### 6. API minimum tarih kuralını uygulamıyor

Geçmiş tarihli bir sorgu `InvalidDepartureDate` değil, `Success` ve 150 sefer döndürüyor. Geçersiz bir lokasyon kimliği de `InvalidLocation` değil, 0 sonuçlu `Success` veriyor. Yani doğrulamanın tek gerçek uygulayıcısı bu uygulama.

### 7. `language` bir görüntü dili değil, pazar seçici

| Değer | Dönen lokasyonlar |
|---|---|
| `tr-TR` | Türk lokasyonları, Türkçe |
| `en-US` | Türk lokasyonları, **İngilizce** |
| `en-GB` | **Britanya** lokasyonları (London, Birmingham…) |
| `de-DE` / `fr-FR` / `ru-RU` | ilgili ülkenin lokasyonları |
| **`en-EN`** (dokümandaki varsayılan) | **istek süresiz askıda kalıyor** |
| `ar-SA`, `xx-XX` | aynı şekilde askıda kalıyor |

İki sonucu var. Birincisi, dokümanın varsayılan olarak verdiği `en-EN` kullanılamaz; dokümanı birebir izleyen bir uygulama kilitlenir. İkincisi, tanınmayan değerler hata döndürmek yerine askıda kaldığı için kullanıcı kontrolündeki bir kültür değerinin API'ye ulaşması erişilebilirlik açığıdır.

Bu nedenle kültür değeri API'ye asla doğrudan geçmiyor; kapalı bir beyaz listeden geçiyor ve bu liste iki katmanda birden uygulanıyor. API istemcisine ayrıca makul bir timeout konuldu. Bkz. `docs/adr/0002`.

### 8. `GetSession` hız sınırlı

API bir CDN arkasında ve oturum oluşturma çağrısı hız sınırlı. Sınır aşıldığında:

```
HTTP/1.1 429 Too Many Requests
Retry-After: 3556
gövde: "error code: 1015"   ← JSON değil
```

Bu, ziyaretçi başına oturum yeniden kullanımını bir optimizasyon değil **zorunluluk** yapıyor: her istekte oturum açan bir uygulama bir saat içinde tamamen bloke olur. Gövdenin JSON olmaması da ayrıca ele alınıyor; ayrıştırma hatası uygulamayı düşürmüyor ve kullanıcıya "hata oluştu" değil "şu an çok fazla istek var" mesajı gösteriliyor.

---

## Mimari

```
Obilet.sln
├── src/
│   ├── Obilet.Web/             ASP.NET Core MVC — controller, view, kaynak dosyaları
│   ├── Obilet.Application/     Servisler, alan modelleri, arayüzler, kurallar
│   └── Obilet.Infrastructure/  obilet API istemcisi, önbellek, yapılandırma
├── tests/
│   ├── Obilet.Tests/           xUnit (201 test)
│   └── js/                     Node test runner (26 test)
└── docs/adr/                   Mimari karar kayıtları
```

Bağımlılık yönü daima içe doğrudur: `Web → Application ← Infrastructure`. `Application` hiçbir HTTP tipine bağlı değildir.

Ayrı bir Domain katmanı **bilinçli olarak yok**: uygulama hiçbir varlığa sahip değil ve tüm veri salt okunur olarak API'den geliyor. Gerekçe `docs/adr/0001`'de.

### Öne çıkan seçimler

| Konu | Karar |
|---|---|
| Oturum | `IDeviceSessionProvider` ziyaretçi başına oturum açar, yeniden kullanır ve geçersizleşirse **bir kez** yeniler |
| Yeniden deneme | `IObiletCallExecutor` tek bir dikiş yeri; servisler oturum yönetimini hiç görmez |
| Hata yönetimi | Global filtre; upstream detay loglanır, asla render edilmez |
| Önbellek | `IDistributedCache` üzerinden, **Market Locale başına** anahtarlı |
| Redis | Opsiyonel bağımlılık; yoksa süreç içi belleğe düşer (`docs/adr/0003`) |
| Lokalizasyon | `.resx` + `IStringLocalizer`, iki dil eksiksiz, bir test pariteyi korur |
| Para biçimi | Sayı biçimi kültürden, para birimi API'nin bildirdiği koddan |
| Tarih hesabı | "Bugün" sunucunun değil **pazarın** saat diliminden okunur (`IMarketClock`) |
| Olanaklar | Sefer kartında ikon olarak; tanıtımlı olanlar (indirim kodları) renkli etiket (`docs/adr/0006`) |
| Günlükleme | Serilog; konsol her zaman, Elasticsearch opsiyonel. ECS biçimi, Kibana ile görüntülenir |
| Tasarım | Verilen masaüstü tasarımlarından, mobil-öncelikli kurulumla (`docs/adr/0007`) |
| Sıralama | Varsayılan sıra sunucuda; liste sayfasındaki sıralama kutusu istemcide çalışır, API'ye yeni istek atmaz |
| İkonlar | Tek bir satır içi SVG sprite; ikon kütüphanesi bağımlılığı yok |

### Alan sözlüğü

`CONTEXT.md` projenin terimlerini tanımlar. En kritik ayrım "oturum" kelimesinin karşıladığı üç farklı kavramın ayrılmasıdır: **Device Session** (obilet kimlik çifti), **Visitor Session** (sunucu taraflı ziyaretçi durumu) ve **ApiClientToken** (sabit header kimliği). Kodda "session" kelimesi tek başına hiçbir yerde geçmez.

---

## Bilinçli olarak kapsam dışı

Bunlar eksiklik değil, tercih:

- **Sayfalama.** Şartname seferlerin sıralı gösterilmesini istiyor; sayfalama veya üst sınır uydurmak veri düşürmüş gibi görünme riski taşıyordu. İnce projeksiyon asıl maliyeti (3,2 MB → ~230 KB) zaten çözdü. Gerçek bir üründe sonraki adım budur.
- **Entegrasyon testleri.** `WebApplicationFactory` ile controller testleri yazılmadı; testler saf mantığa ve API istemcisinin yanıt yorumlamasına odaklandı — projenin gerçek riski orada.
- **Sefer detay sayfası ve satın alma.** Şartname kapsamında değil. Verilen tasarımda kartın sağ ucunda bir "Seç" düğmesi var; düğme görsel olarak duruyor ama **devre dışı** ve sebebi bir açıklama metniyle veriliyor. Çalışıyormuş gibi görünüp hiçbir şey yapan bir düğme, olmayan bir düğmeden daha yanıltıcı olurdu.
- **Koltuk sayısı ve otobüs tipi.** Verilen tasarımların hiçbiri sefer satırında göstermiyor ve karar verirken fiyatı etkileyen bir bilgi taşımıyorlar. Veri modelde mevcut, yalnızca gösterilmiyor.

## Kod incelemesinde bulunan ve düzeltilen kusurlar

Teslim öncesi yapılan incelemede dört kusur bulundu ve hepsi düzeltildi. Hepsi kendi yazdığım koddaydı:

1. **Saat dilimi (orta).** Tarih hesapları sunucunun yerel saatini kullanıyordu. Konteyner imajı UTC çalışıyor, pazar ise UTC+3; ölçülen fark 3 saat. Sonucu: her gece 21:00–00:00 (UTC) arasında sunucunun "bugün"ü kullanıcının dünü oluyor, `Yarın` varsayılanı bugünü gösteriyor ve şartnamenin "minimum geçerli tarih bugündür" kuralı doğrulamadan geçiyordu. Artık tarih pazarın saat diliminden okunuyor; `TZ=Pacific/Auckland` ile çalıştırılan bir konteynerde uygulamanın hâlâ Türkiye tarihini bildirdiği doğrulandı.
2. **Süre biçimi (düşük).** `h:mm` biçimi gün bileşenini düşürüyordu: 25 saat 30 dakikalık bir sefer `1:30` görünüyordu. Ayrıca süre alanı `25:30:00` biçiminde gelirse ayrıştırma hatası **tüm sefer listesini** hata sayfasına çeviriyordu. İkisi de düzeltildi; ayrıştırılamayan süre artık yalnızca gösterilmiyor.
3. **Otomatik tamamlamada bayat eşleşmeler (düşük).** Sunucudan gelen sonuç kimlikleri, terim değiştikten sonra da eşleşme sayılıyordu. Kimlikler artık geldikleri terimle birlikte tutuluyor.
4. **Katlama kurallarının ayrışması (düşük).** İstemci haritası `Í í Î î` karakterlerini atlıyordu, sunucu katlıyordu — "iki uygulama ayrışmamalı" diye yorum yazdığım yerde, daha doğduğu anda ayrışmışlar. Eklendi ve bir test dokuz varyantı birden kontrol ediyor.

Düzeltmeleri yazarken bir de kendi hatamı buldum: `25:30:00` için eklediğim ilk dönüştürücü `TimeSpan.TryParse`'a güveniyordu, ama o `48:00:00` değerini başarısız saymıyor — `d:hh:mm` sanıp **48 gün** olarak ayrıştırıyor. İstisna fırlatmaktan daha kötü bir sonuç. Bileşenler artık elle okunuyor.

## Bilinen sınırlar

- **Türkçe arama katlaması iki yerde:** C# (`TurkishSearchText`) ve JavaScript (`search-form.js`). Tek yerde tutmak mümkün değil, çünkü sunucu API sonucunu, istemci ise sayfayla birlikte gelen varsayılan listeyi süzüyor. Kuralların ayrışmaması gerektiği her iki dosyada yorumla belirtildi.
- **Tarih alanının görünen biçimi** yerel tarayıcı seçicisinden gelir ve işletim sisteminin diline göre belirlenir, sayfanın diline göre değil. Yerel seçiciyi değiştirmek erişilebilirlik ve mobil klavye desteğinden ödün vermek olurdu.
- **`ApiClientToken` `appsettings.json` içinde.** Ödev dokümanında açıkça verildiği ve projenin kurulumsuz çalışması gerektiği için. Gerçek bir üretim ortamında ortam değişkeni veya secret deposu kullanılır.
- **Tanıtım etiketlerinin bazıları düşük kontrastlı.** Renkler API'den geliyor ve renk kararının sahibi operatör kabul edildi. İndirim kodlarında API yeşil bildiriyor ve tasarımdaki görünüm birebir çıkıyor; "Relax Sefer" gibi nitelik etiketlerinde gri bildiriyor ve o gri WCAG AA eşiğinin altında kalıyor. Kendi yeşilimiz yalnızca API renk bildirmediğinde varsayılan olarak kullanılıyor.
- **Sıralama kutusu JavaScript gerektirir.** Varsayılan sıra — kalkış anına göre artan, yani şartnamenin istediği sıra — sunucuda kuruluyor, dolayısıyla JavaScript kapalıyken liste doğru sırada gelir; yalnızca fiyat ve süreye göre yeniden sıralama çalışmaz.
