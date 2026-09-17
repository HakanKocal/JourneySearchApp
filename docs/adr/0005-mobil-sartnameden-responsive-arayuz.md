# Mobil-öncelikli responsive arayüz, mobil-only şartnameden

Verilen Adobe XD tasarım şartnamesi 320x568 boyutunda, `platform: iOS` olarak işaretlenmiş bir **mobil** tasarımdır (Şubat 2019). Paylaşım bağlantısında masaüstü artboard'u yoktur. Şartname ise bir ASP.NET Core MVC **web** uygulaması istiyor.

Arayüz mobil-öncelikli responsive olarak kurulur: 320px tasarımı küçük breakpoint'te sadakatle uygulanır, masaüstünde alanlar canlı obilet.com sitesindeki gibi yatay bir satıra genişler. Yığılı kartlar ve kartların dikiş yerinde duran dairesel takas butonu motifi bu geçişte korunur.

Alternatifler reddedildi: 320px düzenini tüm genişliklerde birebir uygulamak masaüstünde bozuk görünür; mobil şartnameyi yok sayıp sıfırdan masaüstü tasarlamak ise şartnamedeki tek somut tasarım yönlendirmesini çöpe atar.

Bu ADR, arayüzün neden mobil oranlara yakın durduğunu ileride merak edecek okuyucu için vardır — bir tercih değil, eldeki tek tasarım çıktısının sonucudur.

## Consequences

Şartname artboard'u `Bugün` çipini seçili ve tarihi `1 Nisan 2018 Pazar` olarak gösteriyor; bu, "varsayılan tarih yarın olmalı" fonksiyonel gereksinimiyle çelişiyor. Çelişkide fonksiyonel gereksinim esas alınır, artboard yer tutucu kabul edilir.

## Güncelleme

Bu belgenin bir sonucu artık geçerli değil. Yazıldığı sırada, şartname sefer satırında olanak göstermediği için olanak ikonlarının da gösterilmemesi kaydedilmişti. `docs/adr/0006` bu sonucu geçersiz kılıyor: tanıtımlı olanakların indirim kodu olduğu görülünce, tasarıma sadakat fiyatı etkileyen bir bilgiyi saklamaya değmedi.

Belgenin mobil-öncelikli responsive kararı geçerliliğini koruyor.
