# Üç katmanlı çözüm yapısı, Domain katmanı olmadan

Şartname "temiz, modüler, ölçeklenebilir ve yeniden kullanılabilir" kod istiyor ve tasarım desenleri için bonus veriyor, ama aynı zamanda tahmini süreyi ~8 saat olarak veriyor. Çözümü `Obilet.Web` (MVC), `Obilet.Application` (servisler, DTO'lar, arayüzler) ve `Obilet.Infrastructure` (tipli API istemcisi) olarak üç projeye, artı bir test projesine böldük.

Ayrı bir Domain katmanı **bilinçli olarak eklenmedi**. Bu uygulama hiçbir varlığa sahip değil ve hiçbir iş kuralını kendi verisi üzerinde işletmiyor; tüm veri obilet API'sinden geliyor ve salt okunur. Entity'si olmayan bir uygulamaya Domain katmanı eklemek, gösterdiği mimari olgunluktan daha fazla gürültü üretirdi.

Bu uygulamanın gerçek karmaşıklığı API sınırında yaşıyor, dolayısıyla tek anlamlı dikiş yeri (seam) orada: `Obilet.Application` içindeki `IObiletApiClient` soyutlaması, `Obilet.Infrastructure` içindeki somut HTTP uygulamasına bakar. Bağımlılık yönü daima içe doğrudur.

## Considered Options

- **Tek MVC projesi, disiplinli klasörler**: daha az dosya, ama API sınırında test edilebilir bir dikiş yeri bırakmıyor ve şartnamenin modülerlik vurgusuna zayıf yanıt veriyor.
- **MediatR pipeline'lı tam Clean Architecture**: iki sayfalık, durumsuz, salt okunur bir uygulamada CQRS ayrımı savunulamaz; ödünç alınmış karmaşıklık olarak okunur.
