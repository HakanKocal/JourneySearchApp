# Sefer kartında olanaklar gösterilir

Sefer kartına, o seferin olanakları eklendi: kablosuz internet, priz, USB şarj gibi olanaklar ikon olarak, tanıtımlı olanaklar ise renkli metin etiketi olarak.

Bu karar, `docs/adr/0005`'in bir sonucunu **geçersiz kılar**. O belge ve tasarım geçişi sırasındaki karar, olanak ikonlarının gösterilmemesini kaydetmişti; gerekçe, verilen tasarım şartnamesinin sefer satırında yalnızca dört veri göstermesi ve satırın 72 piksel yüksekliğinde olmasıydı.

Kararı değiştiren şey, ilk kararın elinde olmayan bir gözlemdi: tanıtımlı olanaklar süs değil, **ticari bilgi**. Canlı veride bunların çoğu indirim kodu ("75₺ İndirim Kodu", "150₺ İndirim Kodu"), kalanı sefer niteliği ("Jumbo Sefer", "Relax Sefer", "Yeni Otoban"). API bunlara özel zemin ve metin rengi de bildiriyor, yani entegratörün onları ayırt edilebilir biçimde göstermesini bekliyor. Bir indirim kodunun varlığı kullanıcının ödeyeceği tutarı değiştirir; yanıtta gelip ekrana hiç çıkmaması bir eksiklikti.

Tasarım şartnamesine sadakat, kullanıcıdan fiyatı etkileyen bir bilgiyi saklamaya değmedi.

## Consequences

- Tanıtımlı olanaklar ikon değil metin etiketi olarak çizilir: "150₺ İndirim Kodu" bir sembole sığmaz.
- Renkler API'den geldiği ve doğrudan bir `style` özniteliğine yazıldığı için katı bir renk biçimine karşı doğrulanır; tanınmayan değer yok sayılır. Doğrulanmamış bir değer burada enjeksiyon yolu olurdu.
- ~~Kartta gösterilen öğe sayısı sınırlıdır ve sınırlama sunucu tarafında, projeksiyon sırasında uygulanır. Bir seferin 8 olanağı olabiliyor; hepsini her sefer için taşımak, yanıtı 3,2 MB'dan ~230 KB'a indiren kazancı geri alırdı. Kırpılan öğeler bir sayaçla belirtilir.~~ **Bu sonuç `docs/adr/0008` ile geçersiz kılındı.** Ölçüm bu gerekçeyi yanlışladı: kapağın kazandırdığı şey ağ üzerinde 570 bayttı, karşılığında tanıtım etiketi de aynı slotları paylaştığı için indirim kodu olan seferler komşularından bir ikon az gösteriyordu. Artık olanakların tamamı gösteriliyor.
- Olanak adı, API'nin **çevrilen** alanından okunur. Aynı bilgi yanıtta bir düz metin dizisi olarak da bulunuyor ama o dizi İngilizce istekte de Türkçe kalıyor. Yanlış alanı seçmek hata vermez, yalnızca İngilizce sayfada Türkçe metin gösterir.
- Olmayan bir ikon **403** döndürüyor, 404 değil; eksik ikon durum koduna bakılarak değil tarayıcının hata olayıyla gizlenir. Firma logosunda aynı yöntem kullanılıyor.
- `docs/adr/0005`'in mobil-öncelikli responsive kararı geçerliliğini korur; yalnızca "olanaklar gösterilmez" sonucu bu belgeyle değişir.
