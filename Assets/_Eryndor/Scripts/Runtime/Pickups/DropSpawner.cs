// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using Eryndor.Core.Util;
using Eryndor.Data;
using UnityEngine;

namespace Eryndor.Runtime.Pickups
{
    /// <summary>Ölen düşmanın Essence ve loot düşürmesi.</summary>
    public static class DropSpawner
    {
        public static void SpawnDrops(EnemyDefinition enemy, Vector3 position, EssencePickup essencePrefab,
                                      ItemPickup itemPrefab, IRandomSource random)
        {
            if (enemy == null) return;

            if (enemy.EssenceDrop != null && essencePrefab != null &&
                Eryndor.Core.Items.LootRoller.RollChance(enemy.EssenceDropChance, random))
            {
                var e = Object.Instantiate(essencePrefab, position + Vector3.up * 0.8f, Quaternion.identity);
                e.Initialize(enemy.EssenceDrop);
            }

            if (enemy.LootTable == null || itemPrefab == null) return;
            foreach (var drop in enemy.LootTable.Roll(random))
            {
                Vector3 offset = new Vector3(random.NextFloat() - 0.5f, 0f, random.NextFloat() - 0.5f) * 1.5f;
                var item = Object.Instantiate(itemPrefab, position + offset + Vector3.up * 0.3f, Quaternion.identity);
                item.Initialize(drop.Item, drop.Count);
            }
        }
    }
}
