# Sefer listesi sayfasındaki özet kartı arama formuna dönüştü

Sefer listesi sayfasının üstündeki kart, sorgulanan güzergâhı ve günü gösteren bir şeritti. Artık çalışan bir arama formu: aranabilir kalkış ve varış alanları, tarih alanı, hızlı tarih çipleri, takas düğmesi ve bir sorgulama düğmesi taşıyor. Kullanıcı sonuçlara bakarken başka bir şehir veya tarih seçip yeniden sorgulayabiliyor, yani güzergâhı değiştirmek için ana sayfaya dönmesi gerekmiyor.

Alanların davranışı `wwwroot/js/search-fields.js` dosyasına çıkarıldı ve iki sayfa aynı kimliklerle aynı fonksiyonu çağırıyor. Ana sayfanın view'ı 489 satırdan 224 satıra indi. Tek fark başlangıç değerinin kaynağı: liste sayfasında adres, ana sayfada `localStorage`. Depodan okumak, adreste yazan güzergâhla çelişen bir form üretirdi.

## Bu belge `docs/adr/0007`'nin bir sonucunu geçersiz kılıyor

0007 şunu kaydetmişti:

> Liste sayfasındaki tarih çipleri ve yön çevirme ikonu tasarımda görsel öğe; burada gerçek bağlantı. Aynı güzergâhın başka gününe veya ters yönüne tek tıkla gitmek, kullanıcıyı forma geri göndermekten daha az adım.

O karar, kartın salt gösterim olduğu bir dünyada doğruydu. Kart form olunca bağlantılar tutarsız hâle geliyor: kullanıcı açılır listeden yeni bir şehir seçip sonra bir çipe basarsa, bağlantı adresindeki **eski** kimlikleri taşıdığı için seçimi sessizce çöpe gider. Bu yüzden çipler ve takas artık formun alanlarına yazan `type="button"` düğmeleri.

## Consequences

- **Çipler ve takas JavaScript gerektiriyor.** Eskiden bağlantı oldukları için JavaScript kapalıyken de çalışıyorlardı; artık çalışmıyorlar. Formun kendisi etkilenmiyor: alanlar düz `select` ve `input`, gönderim ana sayfanın `Search` eylemine gidiyor ve varsayılan sıralama sunucuda kurulduğu için liste JavaScript kapalıyken de doğru geliyor. Kaybedilen şey iki kısayol; korunan şey seçimin çöpe gitmemesi.

- **Çip artık tek tıkla gitmiyor, iki tık gerekiyor** (çip, sonra sorgulama). 0007'nin "daha az adım" gerekçesi bu yüzden geçerliliğini yitirdi. Karşılığında kullanıcı şehri ve günü birlikte değiştirip tek sorguda gönderebiliyor; eskiden bunun için forma dönmek gerekiyordu.

- **Form ana sayfanın `Search` eylemine gidiyor**, ayrı bir uç nokta açılmadı. Bunun bilinen bir pürüzü var: o eylem hatalı bir sorguda ana sayfanın formunu döndürüyor, dolayısıyla JavaScript kapalıyken geçersiz bir sorgu kullanıcıyı listeden ana sayfaya taşıyor. İstemci tarafı ayna doğrulama açıkken bu yola hiç girilmiyor. README'nin "Bilinen sınırlar" bölümünde kayıtlı.

- **Sorgulanan lokasyon varsayılan 20 kayıtta olmayabiliyor** (`docs/adr/0004`). View o durumda seçeneği elle ekliyor; aksi hâlde tarayıcı seçimi ilk kayda kaydırır ve kullanıcı formu açtığında aradığı güzergâhı görmezdi.

- **Açılır listede çizilen seçenek sayısı sınırlanmıyor.** Bir zamanlar 20 ile sınırlıydı ve tam bu ekleme yüzünden hataya dönüştü: elle eklenen 21. seçenekle birlikte liste 21 kayda çıkıyor, tavan 20'de kaldığı için sondaki şehir çizilmiyordu. Ölçüldü ve düşen kaydın listenin sonundaki "Bartın" olduğu görüldü. Tavanın koruduğu bir şey yoktu; sınır zaten API tarafında.
