// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using System;
using System.Collections.Generic;
using Eryndor.Core.Items;
using Eryndor.Core.Util;
using Eryndor.Core.Validation;
using UnityEngine;

namespace Eryndor.Data
{
    [Serializable]
    public struct ItemLootRow
    {
        public ItemDefinition item;
        [Min(0f)] public float weight;
        [Min(1)] public int minCount;
        [Min(1)] public int maxCount;
    }

    /// <summary>Ağırlıklı loot tablosu. Hesaplama çekirdekteki LootRoller'da yapılır.</summary>
    [CreateAssetMenu(menuName = "Eryndor/Loot Table", fileName = "Loot_")]
    public sealed class LootTableDefinition : DefinitionBase
    {
        [SerializeField, Min(0)] private int rolls = 1;
        [Tooltip("Hiçbir şey düşmeme ağırlığı.")]
        [SerializeField, Min(0f)] private float nothingWeight = 50f;
        [SerializeField] private List<ItemLootRow> rows = new List<ItemLootRow>();

        public List<LootDrop<ItemDefinition>> Roll(IRandomSource random)
        {
            var entries = new List<LootEntry<ItemDefinition>>(rows.Count);
            foreach (var row in rows)
                if (row.item != null)
                    entries.Add(new LootEntry<ItemDefinition>(row.item, row.weight, row.minCount, row.maxCount));
            return LootRoller.Roll(entries, rolls, nothingWeight, random);
        }

        /// <summary>Bir satırın tek atıştaki düşme olasılığı (dengeleme için).</summary>
        public float DropChanceOf(int rowIndex)
        {
            float total = nothingWeight;
            foreach (var r in rows) total += r.weight;
            return total <= 0f ? 0f : rows[rowIndex].weight / total;
        }

        protected override void ValidateFields(ValidationResult r)
        {
            r.Require(rows.Count > 0 || rolls == 0, "Tabloda satır yok.");
            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                r.Require(row.item != null, $"Satır {i}: eşya boş.");
                r.Require(row.maxCount >= row.minCount, $"Satır {i}: maxCount < minCount.");
                r.Recommend(row.weight > 0f, $"Satır {i}: ağırlık 0, hiç düşmez.");
            }
        }

#if UNITY_EDITOR
        public void EditorSet(int rollCount, float nothing, params ItemLootRow[] newRows)
        {
            rolls = rollCount; nothingWeight = nothing; rows = new List<ItemLootRow>(newRows);
        }
#endif
    }
}
