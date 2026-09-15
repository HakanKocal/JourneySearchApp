# "Tüm lokasyonlar" gereksinimi API tarafından karşılanamıyor

Şartname tüm olası Bus Location'ların çekilip Origin ve Destination olarak listelenmesini istiyor. Canlı API üzerinde doğrulandı ki bu **mümkün değil**: `GetBusLocations` çağrısı `data:null` ile tam 20 kayıt döndürüyor (rank 1..20) ve arama sonuçları da 20 ile sınırlı. Sistemdeki gerçek lokasyon sayısı bunun çok üzerinde ve yalnızca arama yoluyla tek tek erişilebiliyor.

Bu nedenle 20 kayıtlık varsayılan liste başlangıç listesi olarak kabul edilir ve geri kalan lokasyonlara erişim, şartnamenin ayrıca zorunlu kıldığı sunucu taraflı metin aramasıyla sağlanır. İki gereksinim bu şekilde uzlaşır.

Alternatif olarak açılışta a–z ve aa–zz gibi terimlerle arama yaparak tam kümeyi kaba kuvvetle çıkarmak düşünüldü ve reddedildi: onlarca ek API çağrısı, eksiksizlik garantisi olmayan bir sonuç ve şartnamenin hiçbir yerde istemediği bir karmaşıklık üretiyordu.

Kısıt README dosyasında açıkça belgelenir. Şartnamenin yazıldığı gibi karşılanamaz olduğunu tespit etmek, sessizce 20 kayıt gösterip gereksinimi karşılanmış saymaktan daha doğru bir davranıştır.

## Consequences

Arama sonuçlarının hiçbir zaman boş dönmediği de doğrulandı — anlamsız girdide API en popüler 10 lokasyona düşüyor. Bu nedenle "sonuç bulunamadı" durumu API yanıtından türetilemez; sunucu tarafında ayrı bir eşleşme doğrulaması gerekir.
