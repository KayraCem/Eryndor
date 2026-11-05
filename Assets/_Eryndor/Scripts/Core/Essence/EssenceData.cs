// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using System;
using System.Collections.Generic;
using Eryndor.Core.Items;

namespace Eryndor.Core.Essence
{
    /// <summary>Bir Essence'ın verebileceği pasif bonus türleri.</summary>
    public enum EssenceBonusType
    {
        FlatStrength,
        FlatAgility,
        FlatVitality,
        MoveSpeedPercent,
        CounterDamagePercent,
        ManaGainPercent,
        PostCounterInvulnerabilitySeconds
    }

    [Serializable]
    public struct EssenceBonus
    {
        public EssenceBonusType type;
        public float value;

        public EssenceBonus(EssenceBonusType type, float value)
        {
            this.type = type;
            this.value = value;
        }
    }

    /// <summary>
    /// Bir Essence'ın çekirdekteki statik tanımı. Unity tarafında EssenceDefinition (ScriptableObject)
    /// bundan bir kopya üretir; böylece çekirdek kod UnityEngine'e bağlı kalmaz.
    /// </summary>
    public sealed class EssenceData
    {
        public string Id { get; }
        public string DisplayName { get; }
        public Rarity Rarity { get; }
        public IReadOnlyList<EssenceBonus> Bonuses { get; }

        public EssenceData(string id, string displayName, Rarity rarity, IReadOnlyList<EssenceBonus> bonuses)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            DisplayName = displayName ?? id;
            Rarity = rarity;
            Bonuses = bonuses ?? Array.Empty<EssenceBonus>();
        }
    }
}
