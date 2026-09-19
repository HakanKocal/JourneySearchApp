# Olanak kapağı kaldırıldı, yanıt sıkıştırma açıldı

Sefer kartında gösterilen olanak sayısındaki dört öğelik sınır kaldırıldı; artık bir seferin bütün olanakları gösteriliyor ve `+N` sayacı yok. Aynı ölçüm sırasında yanıt sıkıştırmanın hiç açılmadığı görüldü ve Brotli ile gzip devreye alındı.

İki karar aynı belgede, çünkü ikisini de aynı ölçüm doğurdu.

## Kapağı kaldıran şey

Kapak bir tutarsızlık üretiyordu. Sınır, tanıtım etiketleriyle ikonları **birlikte** sayıyordu; tanıtım etiketi kartta ikonların üstünde, kendi satırında çizildiği hâlde dört slottan birini tüketiyordu. Sonuç: indirim kodu olan bir sefer, aynı olanaklara sahip komşusundan bir ikon az gösteriyordu. Kullanıcı bunu fark etti — bir kartta dört ikon, hemen altındakinde üç ikon ve bir `+1`.

Kapağın gerekçesi, hepsini taşımanın ince projeksiyondan kazanılan boyutu geri alacağıydı. Ölçüm bunu yanlışladı. 462 seferlik bir liste üzerinde:

| | ham HTML | gzip |
|---|---|---|
| kapak 4 | 2.161.286 bayt | 40.160 bayt |
| kapak yok | 2.226.664 bayt | 40.730 bayt |

Kapak, yanıtın **%1,4'ünü**, ağ üzerinde **570 baytı** kurtarıyordu. Asıl kazanç API yanıtını ince modele indirmekten geliyordu, kapaktan değil; kapağı yazan yorum bu iki şeyi birbirine karıştırmıştı.

Canlı dağılım da kapağı gereksiz kılıyordu: 462 seferin 130'unda hiç olanak yok, 74'ünde dört, 106'sında beş, geri kalanında altı ile dokuz arası. Yani kapak seferlerin yarısından fazlasında devreye giriyordu.

## Kapağın yerini alan şey

Sınır artık veri katmanında değil, yerleşimde. Tarayıcıda 1440px genişlikte ölçüldüğünde 462 kartın tamamı 95px yüksekliğinde çıkıyor — dokuz ikon tek satıra sığıyor, sarma yok.

Bir yerde sarıyordu: 992–1199 bandında Bootstrap'in container'ı 960px ve beş kolonluk düzende olanak kolonuna 96px kalıyor; ikonlar ikinci satıra taşıyor ve kart yükseklikleri 95px ile 135px arasına dağılıyor. Bu yüzden kartın tek satırlık düzeni 992px yerine **1200px**'ten başlıyor. Altındaki genişliklerde kart yığılı düzeni kullanıyor ve olanaklar kendi tam genişlikteki satırını alıyor.

1200px üstünde kalan yükseklik farkı olanaklardan değil içerikten geliyor: 8 kart iki tanıtım etiketi taşıyor, 15 kartta terminal adı ("Ankara (Söğütözü) Cep Terminali") iki satıra sarıyor.

## Sıkıştırma

Kapağı ölçerken sunucunun hiç `Content-Encoding` göndermediği görüldü. 2,2 MB'lık bir HTML'i sıkıştırmasız yollamak, bu ölçekte kazanılabilecek en büyük tek iyileştirmeyi masada bırakmaktı. Ölçülen sonuç:

| | boyut | azalma |
|---|---|---|
| sıkıştırmasız | 2.227.683 bayt | — |
| gzip (Optimal) | 59.660 bayt | %97,3 |
| brotli (Optimal) | 19.952 bayt | %99,1 |

İşaretleme son derece tekrarlı — aynı ikon adresleri, aynı kart yapısı, derin Razor girintileri — dolayısıyla sıkıştırma olağandışı iyi çalışıyor. Yanıt süresi ölçülebilir biçimde değişmiyor; süreyi belirleyen şey zaten upstream API çağrısı (~3 saniye).

Sıkıştırma seviyesi **açıkça** veriliyor. ASP.NET Core'un varsayılanı `Fastest` ve Brotli için bu kalite seviyesi 1 demek: aynı yanıt 193.170 bayt çıkıyor, yani gzip'ten üç kat büyük. Sıkıştırmayı açıp kazancın onda dokuzunu kaybetmek, fark edilmesi zor bir kusur olurdu.

## Consequences

- `Feature.MaxDisplayed`, `Journey.TotalFeatureCount` ve `Journey.HiddenFeatureCount` kaldırıldı. `Journey.Features` artık seferin bütün olanaklarını taşıyor.
- `Journey_MoreFeaturesFormat` ve `Journey_MoreFeaturesTitleFormat` kaynak anahtarları ve `.journey-feature-more` CSS sınıfı kaldırıldı.
- Her ikon adını hem `alt` hem `title` olarak taşıyor: ikonun kendisi ne olduğunu anlatmıyor, üzerine gelince adı görünmeli.
- Sefer kartının tek satırlık düzeni 1200px'ten başlıyor. `docs/adr/0007`'de kaydedilen mobil-öncelikli kurulum değişmedi, yalnızca bu bileşenin breakpoint'i yükseldi.
- `EnableForHttps` varsayılanı olan `false` korunuyor. Sıkıştırılmış bir HTTPS yanıtı BREACH saldırısına açık olabiliyor ve arama formu bir anti-forgery token taşıyor; o token tam olarak bu saldırının hedef aldığı türden bir sırdır. Konteyner HTTP üzerinden (8080) hizmet verdiği için asıl kazanç yine elde ediliyor. TLS sonlandırmanın uygulamanın dışında yapıldığı gerçek bir dağıtımda sıkıştırma da o katmana taşınır.
- `docs/adr/0006`'nın "kartta gösterilen öğe sayısı sınırlıdır" sonucu bu belgeyle geçersiz. O belgenin asıl kararı — olanakların kartta gösterilmesi ve tanıtımlı olanların renkli etiket olması — geçerliliğini koruyor.
