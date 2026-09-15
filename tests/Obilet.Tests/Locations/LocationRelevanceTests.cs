using Obilet.Application.Locations;
using Obilet.Application.Models;

namespace Obilet.Tests.Locations;

/// <summary>
/// Arama sonuçlarının dürüstlük süzgecini doğrular.
/// </summary>
/// <remarks>
/// Bu süzgecin var olma sebebi canlı API'de doğrulanmış bir davranış:
/// <c>GetBusLocations</c> anlamsız bir terime boş liste döndürmüyor,
/// sessizce en popüler lokasyonlara düşüyor. <c>xqjz</c> araması İstanbul ve
/// Ankara döndürüyor. Bunu olduğu gibi göstermek, aranan şey bulunmadığı
/// hâlde bulunmuş gibi bir liste sunmak olurdu.
/// </remarks>
public sealed class LocationRelevanceTests
{
    private static BusLocation Location(int id, string name, string? keywords = null) =>
        new(id, name, id, keywords);

    /// <summary>API'nin anlamsız terimde döndürdüğü popüler lokasyonlar.</summary>
    private static IReadOnlyList<BusLocation> PopularFallback() =>
    [
        Location(349, "İstanbul Avrupa", "Esenler Alibeyköy Otogarı"),
        Location(350, "İstanbul Anadolu", "Harem Otogarı"),
        Location(356, "Ankara", "Aşti Otogarı"),
    ];

    [Fact]
    public void Anlamsiz_terim_bos_sonuc_uretir()
    {
        var filtered = LocationRelevance.Filter(PopularFallback(), "xqjz");

        // API bu terime üç popüler lokasyon döndürüyor; hiçbiri ilişkili
        // değil, dolayısıyla kullanıcıya hiçbir şey gösterilmemeli.
        Assert.Empty(filtered);
    }

    [Fact]
    public void Iliskili_sonuclar_korunur()
    {
        var filtered = LocationRelevance.Filter(PopularFallback(), "istanbul");

        Assert.Equal(2, filtered.Count);
        Assert.All(filtered, location => Assert.Contains("İstanbul", location.Name));
    }

    [Fact]
    public void Iliskisiz_doldurma_kayitlari_ayiklanir()
    {
        // API'nin gerçek davranışı: "ankara" araması Çayırlı ve Gölbaşı gibi
        // anahtar kelime eşleşmeleri de döndürüyor. İlişkili olanlar kalır,
        // tamamen alakasız olanlar atılır.
        IReadOnlyList<BusLocation> results =
        [
            Location(356, "Ankara", "Aşti"),
            Location(1171, "Gölbaşı", "Ankara Gölbaşı"),
            Location(383, "İzmir", "Basmane"),
        ];

        var filtered = LocationRelevance.Filter(results, "ankara");

        Assert.Equal(2, filtered.Count);
        Assert.DoesNotContain(filtered, location => location.Name == "İzmir");
    }

    [Fact]
    public void Anahtar_kelime_uzerinden_eslesme_kabul_edilir()
    {
        // API'nin eşleşmelerinin çoğu keywords alanından geliyor; yalnızca
        // ada bakmak geçerli sonuçları da atardı.
        var results = new[] { Location(349, "İstanbul Avrupa", "Esenler Otogarı Kıraç") };

        var filtered = LocationRelevance.Filter(results, "esenler");

        Assert.Single(filtered);
    }

    [Fact]
    public void Noktasiz_I_ile_arama_noktali_sonucu_bulur()
    {
        var results = new[] { Location(383, "İzmir", null) };

        var filtered = LocationRelevance.Filter(results, "Izmir");

        Assert.Single(filtered);
    }

    [Fact]
    public void API_sirasi_korunur()
    {
        var filtered = LocationRelevance.Filter(PopularFallback(), "istanbul");

        // Sıra API'nin verdiği rank sırasıdır ve süzme onu bozmamalı.
        Assert.Equal([349, 350], filtered.Select(location => location.Id));
    }

    [Fact]
    public void Bos_terim_sonuclari_suzmez()
    {
        // Terim yoksa süzecek bir şey de yok: varsayılan liste çağrısı bu
        // yoldan geçebiliyor ve tüm kayıtlar korunmalı.
        var filtered = LocationRelevance.Filter(PopularFallback(), string.Empty);

        Assert.Equal(3, filtered.Count);
    }

    [Fact]
    public void Bos_giris_listesi_bos_doner()
    {
        Assert.Empty(LocationRelevance.Filter([], "ankara"));
    }

    [Fact]
    public void Tek_kayit_ilgililigi_dogrudan_sorgulanabilir()
    {
        var izmir = Location(383, "İzmir", "Basmane Otogarı");

        Assert.True(LocationRelevance.IsRelevant(izmir, "izmir"));
        Assert.True(LocationRelevance.IsRelevant(izmir, "basmane"));
        Assert.False(LocationRelevance.IsRelevant(izmir, "ankara"));
    }
}
