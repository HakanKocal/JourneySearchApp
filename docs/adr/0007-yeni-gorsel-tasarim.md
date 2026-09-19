# Arayüz, sonradan verilen masaüstü tasarımına göre yenilendi

Arayüz, ödevin sonraki aşamasında verilen iki masaüstü tasarımına göre baştan kuruldu: fotoğraf zeminli bir hero ve onun üzerine binen arama kartı taşıyan ana sayfa, ile aynı hero'nun kısa hâlini ve her seferi tek satırda gösteren yatay kartları taşıyan liste sayfası. Ana sayfaya ayrıca hero'nun altında dört maddelik bir tanıtım şeridi eklendi.

Bu karar `docs/adr/0005`'in **dayanağını** değiştirir. O belge, elde masaüstü tasarımı olmadığı için arayüzün 320x568 boyutundaki mobil Adobe XD şartnamesinden türetildiğini kaydediyordu. Artık masaüstü tasarımı var, dolayısıyla o kısıt ortadan kalktı ve görsel dil — palet, ölçüler, bileşen yerleşimi — yeni tasarımdan alınıyor.

Değişmeyen iki şey var. Birincisi, kurulum hâlâ **mobil-öncelikli**: taban stiller dar ekranı tarif ediyor ve medya sorguları genişlikte verilen masaüstü düzenine açılıyor. Sebebi, bu ürünün trafiğinin mobil olması ve önceki şartnamenin 320px zeminini kaybetmemek. İkincisi, `docs/adr/0006`'nın sefer kartında olanak gösterme kararı aynen korunuyor; yeni tasarım da kartta olanak ikonları ve renkli tanıtım etiketleri gösteriyor, yani iki karar çakışmıyor, örtüşüyor.

## Consequences

- Hero fotoğrafı `wwwroot/img/hero-bus.jpg` olarak projede duruyor. Verilen dosya 1983x793 boyutunda 3,5 MB'lık bir PNG'ydi; bir fotoğraf için yanlış biçim. Kalite 82 ile JPEG'e çevrildi ve 225 KB'a indi (%94 azalma). Hero arka planı sayfanın ilk boyanan öğesi olduğu için bu fark doğrudan algılanan açılış hızına yazılıyor.

- Mavi perde iki farklı gradyan kullanıyor. Geniş ekranda tasarımdaki gibi soldan sağa açılıyor ve ekranın yaklaşık üç çeyreğinde tamamen saydam oluyor. Dar ekranda neredeyse opak: tasarım metni fotoğrafın **yanına** koyarak okunur kılıyor, 390px genişlikte metnin yanında yer yok ve tarayıcıda ölçüldüğünde alt metin dağların üzerinde okunamıyordu.

- Kendi stil dosyamız artık `Styles` bölümünden **sonra** yükleniyor. Sebebi ölçülen bir hata: Tom Select'in CSS'i o bölümden geliyor ve `obilet.css`'ten sonra yüklendiğinde alanın kendi chevron ikonunun yanına bir de kendi açılır liste okunu koyuyordu. İki seçicinin özgüllüğü eşit olduğu için kararı yükleme sırası veriyor. Karşılığı, sayfaya özel bir `Styles` bloğunun `obilet.css`'i ezemeyecek olması; sayfaya özel stiller zaten o dosyada yaşıyor.

- İkonlar tek bir SVG sprite içinde (`Views/Shared/_IconSprite.cshtml`) ve layout içinde bir kez çiziliyor. Satır içi ikon tercih edilmedi: sefer listesi yüzlerce kart içerebiliyor ve her kartta üç ikon var. Bir ikon kütüphanesi de eklenmedi; kullanılan ikon sayısı on üç ve hepsi tek renkli konturdan oluşuyor.

- Liste sayfasına sıralama açılır kutusu eklendi ve sıralama **istemci tarafında** yapılıyor. Liste zaten tamamen sayfada; yeniden sorgulamak obilet API'sine gereksiz bir istek olurdu ve o API hız sınırı uyguluyor. Varsayılan sıra kalkış anına göre artan ve sunucuda kuruluyor, yani bu bir gereksinim olarak korunuyor ve JavaScript kapalıyken de geçerli. Ertesi güne taşan seferleri ayıran başlık yalnızca kronolojik sırada anlam taşıdığı için başka bir sıralamada gizleniyor.

- Kartlardaki "Seç" düğmesi tasarımda var ama **devre dışı**. Koltuk seçimi ve satın alma bu uygulamanın kapsamı dışında; çalışıyormuş gibi görünen bir düğme kullanıcıya ve değerlendiriciye yanlış bilgi verirdi. Devre dışı olduğu `disabled` ile yardımcı teknolojilere de bildiriliyor ve sebebi bir açıklama metniyle veriliyor.

- Liste sayfasındaki tarih çipleri ve yön çevirme ikonu tasarımda görsel öğe; burada gerçek bağlantı. Aynı güzergâhın başka gününe veya ters yönüne tek tıkla gitmek, kullanıcıyı forma geri göndermekten daha az adım.

- Dil değiştirici sayfanın altından hero'nun sağ üst köşesine taşındı ve kendi yarı saydam zeminini taşıyor. Zemin bir süs değil: mavi perde ekranın sağına ulaşmadığı için değiştirici fotoğrafın en parlak kısmının üzerine düşüyor ve beyaz metin kayboluyordu. Hero'su olmayan sayfalarda (hata ekranları) alt bilgideki kopya duruyor.

- Tanıtım etiketlerinin renkleri hâlâ API'den geliyor ve `docs/adr/0006`'daki doğrulamadan geçiyor. Tasarım bu etiketi yeşil gösteriyor ve indirim kodlarında API gerçekten yeşil bildiriyor; ancak "Relax Sefer" gibi nitelik etiketlerinde gri bildiriyor ve o gri, tasarımdaki yeşil kadar okunur değil. Renk kararının sahibi operatör kabul edildi; kendi yeşilimiz yalnızca API renk bildirmediğinde varsayılan olarak kullanılıyor.
