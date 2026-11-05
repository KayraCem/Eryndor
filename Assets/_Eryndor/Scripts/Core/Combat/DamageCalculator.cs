// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using System;
using Eryndor.Core.Stats;

namespace Eryndor.Core.Combat
{
    /// <summary>Hasar hesabı. Tüm çarpanlar tek yerde toplanır.</summary>
    public static class DamageCalculator
    {
        /// <summary>
        /// Son hasar = taban × STR çarpanı × ek çarpan − düz savunma (en az 0).
        /// </summary>
        public static float Compute(CombatConfig config, float baseDamage, int attackerStrength,
                                    float extraMultiplier = 1f, float targetDefense = 0f)
        {
            float raw = baseDamage * StatFormulas.DamageMultiplier(config, attackerStrength) * extraMultiplier;
            return Math.Max(0f, raw - Math.Max(0f, targetDefense));
        }

        /// <summary>Başarılı counter'ın hasarı: counter çarpanı ve Essence'tan gelen yüzde bonusla.</summary>
        public static float CounterDamage(CombatConfig config, float baseDamage, int attackerStrength,
                                          float counterBonusPercent, float targetDefense = 0f)
        {
            float multiplier = config.counterDamageMultiplier * (1f + counterBonusPercent / 100f);
            return Compute(config, baseDamage, attackerStrength, multiplier, targetDefense);
        }
    }
}
