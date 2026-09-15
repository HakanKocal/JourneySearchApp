using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Obilet.Infrastructure.Obilet;

/// <summary>
/// <c>hh:mm:ss</c> biçimindeki süreleri, saat bileşeni 24'ü aştığında da
/// okuyabilen dönüştürücü.
/// </summary>
/// <remarks>
/// <para>
/// Varsayılan <see cref="System.Text.Json"/> dönüştürücüsü yalnızca sabit
/// biçimi (<c>[-][d.]hh:mm:ss</c>) kabul ediyor ve saat bileşeninin 00–23
/// aralığında olmasını bekliyor. Ölçülen davranış: <c>"1.01:30:00"</c>
/// ayrıştırılıyor, <c>"25:30:00"</c> ise <see cref="JsonException"/>
/// fırlatıyor.
/// </para>
/// <para>
/// Bu bir uçurum oluşturuyordu: API doküman <c>duration</c> alanını
/// <c>HH:MM:SS</c> olarak belgeliyor ve 24 saati aşan bir sefer için
/// <c>25:30:00</c> göndermesi mümkün. O durumda ayrıştırma hatası tüm
/// yanıtı düşürüyor ve kullanıcı tek bir tuhaf süre yerine <b>sefer
/// listesinin tamamı</b> yerine hata sayfası görüyordu.
/// </para>
/// <para>
/// Dönüştürücü önce sabit biçimi dener, sonra saat bileşenini gün ve saate
/// bölerek yeniden kurar. Hiçbiri olmazsa <c>null</c> döner: süre
/// gösterilemeyen bir ayrıntı, yanıtı düşürmeye değmez.
/// </para>
/// </remarks>
internal sealed class TolerantTimeSpanConverter : JsonConverter<TimeSpan?>
{
    public override TimeSpan? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.String)
        {
            return null;
        }

        var value = reader.GetString();

        return Parse(value);
    }

    public override void Write(
        Utf8JsonWriter writer,
        TimeSpan? value,
        JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value.ToString("c", CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Süre metnini ayrıştırır; ayrıştırılamazsa <c>null</c> döner.
    /// </summary>
    internal static TimeSpan? Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var text = value.Trim();

        // Gün bileşeni açıkça yazılmışsa ("1.01:30:00") sabit biçim güvenli.
        if (text.Contains('.', StringComparison.Ordinal)
            && TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out var withDays))
        {
            return withDays;
        }

        // İki nokta üst üste ile ayrılmış biçim elle ayrıştırılır.
        //
        // Burada TimeSpan.TryParse'a güvenilemez: "48:00:00" değerini
        // başarısız saymıyor, "d:hh:mm" sanıp 48 GÜN olarak ayrıştırıyor.
        // Bu, istisna fırlatmasından daha kötü — sessizce yanlış bir süre
        // üretiyor. API dokümanı alanı HH:MM:SS olarak belgelediği için
        // bileşenleri kendimiz okuyoruz. (Bu davranışı yazdığım bir test
        // ortaya çıkardı.)
        var parts = text.Split(':');
        if (parts.Length is < 2 or > 3)
        {
            return null;
        }

        if (!int.TryParse(parts[0], CultureInfo.InvariantCulture, out var hours)
            || !int.TryParse(parts[1], CultureInfo.InvariantCulture, out var minutes))
        {
            return null;
        }

        var seconds = 0;
        if (parts.Length == 3
            && !int.TryParse(parts[2], CultureInfo.InvariantCulture, out seconds))
        {
            return null;
        }

        if (hours < 0 || minutes is < 0 or > 59 || seconds is < 0 or > 59)
        {
            return null;
        }

        return new TimeSpan(hours, minutes, seconds);
    }
}
