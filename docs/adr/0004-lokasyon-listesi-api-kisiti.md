# "Tüm lokasyonlar" gereksinimi API tarafından karşılanamıyor

Şartname tüm olası Bus Location'ların çekilip Origin ve Destination olarak listelenmesini istiyor. Canlı API üzerinde doğrulandı ki bu **mümkün değil**: `GetBusLocations` çağrısı `data:null` ile tam 20 kayıt döndürüyor (rank 1..20) ve arama sonuçları da 20 ile sınırlı. Sistemdeki gerçek lokasyon sayısı bunun çok üzerinde ve yalnızca arama yoluyla tek tek erişilebiliyor.

Bu nedenle 20 kayıtlık varsayılan liste başlangıç listesi olarak kabul edilir ve geri kalan lokasyonlara erişim, şartnamenin ayrıca zorunlu kıldığı sunucu taraflı metin aramasıyla sağlanır. İki gereksinim bu şekilde uzlaşır.

Alternatif olarak açılışta a–z ve aa–zz gibi terimlerle arama yaparak tam kümeyi kaba kuvvetle çıkarmak düşünüldü ve reddedildi: onlarca ek API çağrısı, eksiksizlik garantisi olmayan bir sonuç ve şartnamenin hiçbir yerde istemediği bir karmaşıklık üretiyordu.

Kısıt README dosyasında açıkça belgelenir. Şartnamenin yazıldığı gibi karşılanamaz olduğunu tespit etmek, sessizce 20 kayıt gösterip gereksinimi karşılanmış saymaktan daha doğru bir davranıştır.

## Consequences

Arama sonuçlarının hiçbir zaman boş dönmediği de doğrulandı — anlamsız girdide API en popüler 10 lokasyona düşüyor. Bu nedenle "sonuç bulunamadı" durumu API yanıtından türetilemez; sunucu tarafında ayrı bir eşleşme doğrulaması gerekir.

## Bu kısıtın yol açtığı bir hata ve düzeltmesi

Sefer sayfası, sorgulanan lokasyonların adlarını bu 20 kayıtlık listeden çözüyordu. Listede olmayan bir lokasyon seçildiğinde ad bulunamıyor ve arayüz kimliğe düşüyordu: kullanıcı arama yoluyla Rize'yi seçtiğinde sefer sayfasının özet kartında "Rize" değil **"400"** görüyordu. Kısıtın kendisi kabul edilmişti ama bu sonucu fark edilmemişti.

Düzeltme, adın **doğru kaynağını** bulmakla geldi: API her sefer kaydında `origin-location` ve `destination-location` alanlarını zaten gönderiyor. Bu alanlar varsayılan listeden bağımsız, sorguya özel ve Market Locale'e göre çevrilmiş hâlde geliyor. Yani ad artık listeden değil seferin kendisinden okunuyor ve 20 kayıt kısıtı sefer sayfasını hiç etkilemiyor.

Karıştırılmaması gereken bir ayrım var: `journey.origin` **terminalin** adını taşıyor ("Esenler Otogarı"), `origin-location` ise lokasyonun adını ("İstanbul Avrupa"). Kartta ikisi de gösteriliyor ve yanlış alanı seçmek hata vermez, yalnızca ekranda yanlış metin gösterir. Bir test iki alan çiftini bilinçli olarak farklı değerlerle besleyerek bunu koruyor.

Geriye tek bir boşluk kalıyor: sorgu **hiç sefer döndürmezse** okunacak kayıt da yok ve lokasyon varsayılan listede değilse ad yine kimliğe düşer. API kimlikten ada çözüm yapan bir uç nokta sunmadığı için (kimlikle arama denendi, boş dönüyor) bu durumda elde daha iyi bir kaynak yok.
