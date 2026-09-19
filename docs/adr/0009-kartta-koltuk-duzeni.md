# Sefer kartında koltuk düzeni gösterilir

Sefer kartı artık aracın koltuk düzenini (`2+1`, `2+2`) yanında bir koltuk ikonuyla gösteriyor. Değer API'nin `bus-type` alanından geliyor ve bir süredir modelde taşınıyordu ama hiçbir yerde gösterilmiyordu.

Bu, verilen masaüstü tasarımlarında **olmayan** bir eklemedir; `docs/adr/0007`'nin kaydettiği tasarıma sadakatten bilinçli bir sapmadır. Gerekçe: 2+1 ile 2+2 arasındaki fark yolcunun aldığı şeyi doğrudan değiştiriyor ve aynı hatta aynı fiyata iki farklı düzen satılabiliyor. Fiyatı etkileyen bir bilgiyi gizlememe gerekçesi `docs/adr/0006`'da olanaklar için kurulmuştu; aynı gerekçe burada da geçerli.

Aynı sebeple `README.md`'deki "koltuk sayısı ve otobüs tipi kapsam dışı" notu artık yalnızca koltuk sayısı için geçerli.

## Nerede ve neden orada

Koltuk düzeni, olanakların yanına değil **sürenin** yanına konuldu ve süreyle aynı sınıfı (`.journey-spec`) paylaşıyor. İkisi de seferin değişmez bir ölçüsü; olanaklar ise opsiyonel donanım. Olanak ikonlarının arasına bir metin değeri koymak iki farklı türden bilgiyi aynı görsel dile sıkıştırırdı.

Yanındaki koltuk ikonu süs değil: `2+1` kendi başına anlamı olmayan bir dizedir ve ikon onu bağlamına oturtuyor. Tam karşılığı ("Koltuk düzeni: 2+1") ipucu metninde duruyor, çünkü kartta yer alan mikro etiket dört etiketle satırı paylaştığı için kısa tutulmak zorunda.

## Kart ızgarası yeniden düzenlendi

Eklemenin ölçülen bir bedeli vardı ve o bedel kaldırıldı.

Kart dar ekranda `"trip price"` düzenindeydi: saatler ve terminaller tek bir sarmalayıcı içinde, fiyat onların sağında. 390px genişlikte o sarmalayıcıya 202px kalıyor, saatler satırının doğal genişliği ise dördüncü hücreyle 246px oluyordu. Sonuç: içerik sıkışıyor, sarmaya izin verilince de 462 kartlık listede kart başına **37px**, yani %19 uzunluk ekleniyordu.

Sarmalayıcı kaldırıldı; saatler ve terminaller iki ayrı ızgara öğesi oldu. Dar ekranda:

```
"times    times"
"stations price"
```

Saatler kartın tam genişliğini alıyor (315px), fiyat terminallerin yanına iniyor. 1200px üstünde ikisi sol kolonda iki satır hâlinde duruyor ve diğer kolonlar o iki satırı birden kaplıyor.

Ölçülen sonuç: koltuk düzeni eklemek 390px, 1000px ve 1440px genişliklerin **hiçbirinde** ortalama kart yüksekliğini değiştirmiyor.

## Koltuk ikonu yeniden çizildi

Sprite'taki koltuk sembolü yandan görünüm olarak çizilmişti. 72 piksele büyütülüp bakıldığında koltuk gibi değil, kutu üstünde bir etiket gibi okunuyordu — 24 pikselde de aslında okunmuyordu, yalnızca fark edilmiyordu. Önden görünüme çevrildi: sırtlık, iki kolçak, gövde ve iki ayak. Bu siluet 16 pikselde de tanınıyor ve ikon kartta o boyutta kullanılıyor.

Değişiklik ana sayfadaki "Rahat Koltuklar" maddesini de düzeltiyor; aynı sembolü 24 pikselde kullanıyor.

## Consequences

- `Journey.BusType` artık görüntülenen bir alan. Bir eşleme testiyle korunuyor: değer olduğu gibi taşınıyor, alan hiç gelmediğinde ve `null` geldiğinde boş kalıyor.
- API alanı bildirmezse hücre hiç çizilmiyor. Etiketi olup değeri olmayan bir hücre, veri eksikliğini bir arayüz hatası gibi gösterirdi.
- Canlı veride ölçülen dağılım: 464 seferin 463'ü `2+1`, biri `2+2`; hiçbirinde alan eksik değil. Yani boş durum yolu pratikte devreye girmiyor ama test ediliyor.
- Saatler satırında sarma açık bırakıldı. Dar ekranda dördü rahatça sığıyor, ama beklenmedik biçimde uzun bir değer geldiğinde içerik kırpılmak yerine alta düşüyor.
- Koltuk **sayısı** (`total-seats`, `available-seats`) hâlâ gösterilmiyor; kapsam dışı kalma gerekçesi değişmedi.
