// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using System.Collections.Generic;
using Eryndor.Core.Util;

namespace Eryndor.Tests
{
    /// <summary>Önceden verilen sayıları sırayla döndüren sahte rastgelelik; testler tekrarlanabilir olsun diye.</summary>
    public sealed class FakeRandom : IRandomSource
    {
        private readonly Queue<float> _floats;
        public int LastIntMin, LastIntMax;
        public int IntToReturn = -1; // -1: min değeri döndür

        public FakeRandom(params float[] floats) { _floats = new Queue<float>(floats); }

        public float NextFloat() => _floats.Count > 0 ? _floats.Dequeue() : 0f;

        public int NextInt(int minInclusive, int maxExclusive)
        {
            LastIntMin = minInclusive; LastIntMax = maxExclusive;
            return IntToReturn < 0 ? minInclusive : System.Math.Min(System.Math.Max(IntToReturn, minInclusive), maxExclusive - 1);
        }
    }
}
