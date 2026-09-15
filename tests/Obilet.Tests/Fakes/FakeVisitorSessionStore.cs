using Obilet.Application.Abstractions;

namespace Obilet.Tests.Fakes;

/// <summary>
/// Visitor Session deposunun bellek içi karşılığı.
/// </summary>
/// <remarks>
/// Elle yazıldı; bir mock kütüphanesi eklemeye değmeyecek kadar küçük ve
/// testlerin ne beklediğini okumak bu hâliyle daha kolay.
/// </remarks>
internal sealed class FakeVisitorSessionStore : IVisitorSessionStore
{
    private readonly Dictionary<string, string> _values = [];

    public string? Get(string key) => _values.GetValueOrDefault(key);

    public void Set(string key, string value) => _values[key] = value;

    public void Remove(string key) => _values.Remove(key);

    /// <summary>Depoda tutulan anahtar sayısı.</summary>
    public int Count => _values.Count;
}
