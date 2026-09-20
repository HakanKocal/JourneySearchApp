namespace Obilet.Web.Models;

/// <summary>
/// <c>_SearchFieldsScripts</c> partial'ının girdisi: arama alanlarının
/// istemci tarafı kurulumunda iki sayfa arasında farklılaşan değerler.
/// </summary>
/// <remarks>
/// <para>
/// Partial'ın var olma sebebi tekrar: ana sayfa ve sefer listesi sayfası
/// aynı üç script etiketini ve aynı yapılandırma nesnesini taşıyordu —
/// arama adresi, minimum terim uzunluğu ve beş mesaj. Bu repoda tam bu
/// kalıptan bir kez zarar görüldü: Türkçe katlama kuralları iki yere
/// dağılmıştı ve bir kod incelemesi ayrıştıklarını yakaladı. Mesaj listesi
/// de sessizce ayrışabilecek türden.
/// </para>
/// <para>
/// Öğe kimlikleri bilinçli olarak burada değil: iki sayfa da aynı
/// kimlikleri kullanıyor ve o kimlikler partial ile view'lar arasındaki
/// sözleşme. Parametreye çevirmek, hiçbir çağıranın farklılaşmadığı bir
/// esnekliği taşımak olurdu.
/// </para>
/// </remarks>
/// <param name="Today">
/// Pazarın saat diliminde bugünün tarihi. İstemcinin karşılaştırmalarda
/// kullandığı ISO tarihler buradan üretilir; bkz. <c>IMarketClock</c>.
/// </param>
/// <param name="RestoreLastSearch">
/// Son arama depodan geri yüklenecek mi?
/// </param>
/// <remarks>
/// <paramref name="RestoreLastSearch"/> ana sayfada <c>true</c>, sefer
/// listesi sayfasında <c>false</c>. Liste sayfasının başlangıç değeri
/// adresin kendisidir; depodan okumak adreste yazan güzergâhla çelişen bir
/// form üretirdi.
/// </remarks>
/// <param name="NoticeElementId">
/// Eskimiş tarihin ileriye çekildiğini bildiren uyarının kimliği; yalnızca
/// geri yükleme yapan sayfada anlamlı, diğerinde <c>null</c>.
/// </param>
public sealed record SearchFieldsScripts(
    DateOnly Today,
    bool RestoreLastSearch,
    string? NoticeElementId = null);
