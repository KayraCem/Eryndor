// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using System;
using System.Collections.Generic;
using Eryndor.Core.Util;

namespace Eryndor.Core.Items
{
    /// <summary>Ağırlıklı loot tablosunun bir satırı. T = eşya tanımı.</summary>
    public readonly struct LootEntry<T>
    {
        public readonly T Item;
        public readonly float Weight;
        public readonly int MinCount;
        public readonly int MaxCount;

        public LootEntry(T item, float weight, int minCount = 1, int maxCount = 1)
        {
            Item = item;
            Weight = weight;
            MinCount = Math.Max(1, minCount);
            MaxCount = Math.Max(MinCount, maxCount);
        }
    }

    public readonly struct LootDrop<T>
    {
        public readonly T Item;
        public readonly int Count;
        public LootDrop(T item, int count) { Item = item; Count = count; }
    }

    /// <summary>
    /// Ağırlıklı rastgele seçim. "nothingWeight" hiçbir şey düşmeme ihtimalidir.
    /// Örnek: iksir 30, kristal 20, hiçbir şey 50 → %30 / %20 / %50.
    /// </summary>
    public static class LootRoller
    {
        public static List<LootDrop<T>> Roll<T>(IReadOnlyList<LootEntry<T>> entries, int rolls,
                                                float nothingWeight, IRandomSource random)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));
            var drops = new List<LootDrop<T>>();
            if (entries == null || entries.Count == 0 || rolls <= 0) return drops;

            float total = Math.Max(0f, nothingWeight);
            foreach (var e in entries) total += Math.Max(0f, e.Weight);
            if (total <= 0f) return drops;

            for (int r = 0; r < rolls; r++)
            {
                float pick = random.NextFloat() * total;
                foreach (var e in entries)
                {
                    float w = Math.Max(0f, e.Weight);
                    if (w <= 0f) continue;
                    if (pick < w)
                    {
                        int count = random.NextInt(e.MinCount, e.MaxCount + 1);
                        drops.Add(new LootDrop<T>(e.Item, count));
                        break;
                    }
                    pick -= w;
                }
                // Döngü bir şey seçmeden biterse "hiçbir şey" çıkmıştır.
            }
            return drops;
        }

        /// <summary>Basit yüzde şansı: chance 0..1.</summary>
        public static bool RollChance(float chance, IRandomSource random) =>
            chance > 0f && random.NextFloat() < chance;
    }
}
