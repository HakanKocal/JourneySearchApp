# Market Locale beyaz listesi ve `en-EN` yasağı

obilet API'sinin `language` alanı bir görüntü dili değil, **pazar seçicidir**. Canlı API üzerinde doğrulanan davranış:

- `tr-TR` → Türk lokasyonları, Türkçe adlarla
- `en-US` → Türk lokasyonları, İngilizce adlarla (`Istanbul Europe`)
- `en-GB` → **Britanya** lokasyonları (`London`, `Birmingham`)
- `de-DE` → Alman lokasyonları, `fr-FR` → Fransız, `ru-RU` → Rus
- `en-EN` → **istek süresiz askıda kalıyor** (2/2 denemede 25 saniyede 0 byte)
- `ar-SA`, `xx-XX` gibi tanınmayan değerler → aynı şekilde askıda kalıyor

İki sonuç doğurdu. Birincisi, resmî API dokümanının varsayılan olarak belirttiği `en-EN` değeri kullanılamaz durumda; dokümanı birebir izleyen bir uygulama açılışta kilitlenir. İkincisi ve daha önemlisi, tanınmayan locale değerleri hata döndürmek yerine askıda kaldığı için, kullanıcı kontrolündeki serbest metin bir kültür değerinin API'ye ulaşması bir erişilebilirlik açığıdır: `?culture=xx-XX` ile sunucu thread'leri tüketilebilir.

Bu nedenle Display Culture değeri API'ye **asla doğrudan geçirilmez**. Aradaki eşleme açık bir beyaz listedir (`tr-TR` → `tr-TR`, `en-*` → `en-US`, tanınmayan → `tr-TR`) ve bu eşleme tek bir yerde yaşar. Beyaz liste hem MVC kültür çözümlemesinde hem de API istemcisinin içinde ayrı ayrı uygulanır; ikinci katman, MVC tarafında bir gedik açılırsa diye vardır.

`en-GB`'nin desteklenen diller arasında olmaması da bilinçlidir: kullanıcıyı sessizce Britanya otobüs hatlarına düşürürdü.

## Consequences

- API istemcisine makul bir `Timeout` (~15s) zorunludur; varsayılan 100s, bu davranışla birlikte istek yığılmasına açıktır.
- Yeni bir dil eklemek `.resx` dosyası eklemekle bitmez — beyaz listeye de eklenmesi ve gerçek bir API çağrısıyla doğrulanması gerekir.
