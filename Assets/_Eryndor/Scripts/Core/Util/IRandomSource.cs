// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using System;

namespace Eryndor.Core.Util
{
    /// <summary>
    /// Rastgelelik kaynağı. Testlerde sabit sonuç veren bir sürüm verilebilsin diye arayüz olarak tutulur.
    /// </summary>
    public interface IRandomSource
    {
        /// <summary>[0, 1) aralığında sayı.</summary>
        float NextFloat();

        /// <summary>[minInclusive, maxExclusive) aralığında tam sayı.</summary>
        int NextInt(int minInclusive, int maxExclusive);
    }

    /// <summary>System.Random tabanlı varsayılan kaynak. Seed verilirse tekrarlanabilir.</summary>
    public sealed class SystemRandomSource : IRandomSource
    {
        private readonly Random _random;

        public SystemRandomSource() { _random = new Random(); }
        public SystemRandomSource(int seed) { _random = new Random(seed); }

        public float NextFloat() => (float)_random.NextDouble();
        public int NextInt(int minInclusive, int maxExclusive) => _random.Next(minInclusive, maxExclusive);
    }
}
