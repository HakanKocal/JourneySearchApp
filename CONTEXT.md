# Bus Journey Search

obilet.com business API'si üzerinden şehirler arası otobüs seferi arayan iki sayfalık bir web uygulaması. Kullanıcı bir kalkış noktası, bir varış noktası ve bir tarih seçer; uygulama o sorgu için uygun seferleri listeler. Uygulama bilet satmaz, rezervasyon yapmaz ve hiçbir veriye sahip değildir — tüm veri obilet API'sinden gelir.

## Language

### Arama (Search)

**Bus Location**:
Bir seferin başlangıç veya bitiş noktası olabilen coğrafi yer. obilet sisteminde tümü `Town` tipindedir; şehirden daha küçük bir ilçe/semt granülerliğini temsil eder (örneğin İstanbul, Avrupa ve Anadolu olarak ikiye ayrılır).
_Kaçınılacak: city, şehir, station, terminal, durak_

**Origin**:
Kullanıcının seçtiği kalkış Bus Location'ı. Arayüzde "Nereden".
_Kaçınılacak: from, source, başlangıç, kalkış yeri_

**Destination**:
Kullanıcının seçtiği varış Bus Location'ı. Arayüzde "Nereye".
_Kaçınılacak: to, target, arrival, varış yeri_

**Departure Date**:
Kullanıcının sefer aradığı takvim günü. Saat bileşeni taşımaz; bir gündür, bir andır değil.
_Kaçınılacak: travel date, journey date, tarih_

**Search Query**:
Bir Origin, bir Destination ve bir Departure Date üçlüsü. Uygulamanın sefer listesi üretmek için ihtiyaç duyduğu tam girdi.
_Kaçınılacak: request, criteria, filter, sorgu_

### Sefer (Journey)

**Journey**:
Belirli bir Partner tarafından, belirli bir Origin'den belirli bir Destination'a, belirli bir kalkış anında işletilen tek bir otobüs seferi. Arayüzde "Sefer".
_Kaçınılacak: trip, service, ride, bus, sefer kaydı_

**Partner**:
Bir Journey'i işleten otobüs firması. Kendi markası, logosu ve puanı vardır.
_Kaçınılacak: company, operator, carrier, firm, firma_

**Stop**:
Bir Journey'in güzergâhı üzerinde durduğu yer. Bunlardan biri istenen Origin'i, bir diğeri istenen Destination'ı temsil eder; geri kalanı ara duraklardır.
_Kaçınılacak: station, terminal, waypoint, durak_

**Feature**:
Bir Journey'de sunulan olanak; örneğin kablosuz internet veya priz. Kullanıcıya gösterilebilir bir adı ve ikonu vardır.
_Kaçınılacak: amenity, facility, service, özellik_

**Internet Price**:
Bir Journey için çevrimiçi satış fiyatı. Kullanıcının fiilen ödeyeceği tutar budur.
_Kaçınılacak: price, fare, sale price, net price_

**Original Price**:
Bir Journey'in indirim öncesi liste fiyatı. Internet Price'tan yüksek olabilir; eşit olduğunda indirim yoktur.
_Kaçınılacak: base price, full price, list price, liste fiyatı_

### Kimlik ve oturum

Bu üç terim günlük dilde "oturum" olarak anılır ve birbirine karıştırılırsa kod okunamaz hâle gelir. Ayrım bağlayıcıdır.

**Device Session**:
obilet API'sinin, adına çağrı yapılabilmesi için talep ettiği kimlik çifti. Uygulamanın son kullanıcı adına API ile konuşma yetkisini temsil eder. Bir kimlik bilgisidir ve hiçbir koşulda tarayıcıya ulaşmaz.
_Kaçınılacak: session, token, credentials, api session, oturum_

**Visitor Session**:
Uygulamayı ziyaret eden tek bir son kullanıcının, o kullanıcıya ait Device Session'ı da kapsayan sunucu taraflı durumu. Her farklı ziyaretçinin kendi Visitor Session'ı vardır.
_Kaçınılacak: user session, http session, kullanıcı oturumu_

**ApiClientToken**:
obilet tarafından uygulamaya verilen, uygulamanın kendisini API'ye tanıttığı sabit kimlik. Ziyaretçiye özel değildir; tüm kullanıcılar için aynıdır.
_Kaçınılacak: api key, secret, auth token, bearer token_

### Dil ve pazar

**Market Locale**:
obilet API'sine gönderilen, hangi ülkenin lokasyon kataloğunun ve hangi dildeki metinlerin döneceğini birlikte belirleyen değer. Bir görüntü dili değildir: pazar seçicidir. Farklı bir Market Locale, farklı bir ülkenin şehirlerini döndürür.
_Kaçınılacak: language, culture, locale, dil_

**Display Culture**:
Uygulamanın kendi arayüz metinlerini, tarih ve para birimi biçimlerini belirleyen kullanıcı tercihi. Market Locale'e eşlenir ama onunla aynı şey değildir.
_Kaçınılacak: language, ui language, arayüz dili_
