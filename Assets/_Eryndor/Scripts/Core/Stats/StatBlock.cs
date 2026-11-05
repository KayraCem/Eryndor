// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using System;

namespace Eryndor.Core.Stats
{
    /// <summary>
    /// Üç temel stat. Diğer tüm değerler (can, hasar, counter penceresi) bunlardan formülle türetilir.
    /// </summary>
    [Serializable]
    public struct StatBlock
    {
        public int strength;   // Güç: vuruş hasarı
        public int agility;    // Çeviklik: counter penceresi
        public int vitality;   // Dayanıklılık: maksimum can

        public StatBlock(int strength, int agility, int vitality)
        {
            this.strength = strength;
            this.agility = agility;
            this.vitality = vitality;
        }

        public static StatBlock operator +(StatBlock a, StatBlock b) =>
            new StatBlock(a.strength + b.strength, a.agility + b.agility, a.vitality + b.vitality);

        public override string ToString() => $"STR {strength} / AGI {agility} / VIT {vitality}";
    }
}
