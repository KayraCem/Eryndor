// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using System;
using Eryndor.Core.Combat;

namespace Eryndor.Core.Stats
{
    /// <summary>
    /// Türetilmiş değerlerin formülleri. Hepsi saf fonksiyon: aynı girdi her zaman aynı sonucu verir,
    /// bu yüzden doğrudan birim testiyle kontrol edilir.
    /// </summary>
    public static class StatFormulas
    {
        /// <summary>MaxHP = baseHp + hpPerVitality × VIT</summary>
        public static float MaxHp(CombatConfig c, int vitality) =>
            c.baseHp + c.hpPerVitality * Math.Max(0, vitality);

        /// <summary>Hasar çarpanı = 1 + damagePerStrength × STR</summary>
        public static float DamageMultiplier(CombatConfig c, int strength) =>
            1f + c.damagePerStrength * Math.Max(0, strength);

        /// <summary>Counter penceresi = min(taban + AGI × artış, üst sınır), saniye.</summary>
        public static float CounterWindow(CombatConfig c, int agility) =>
            Math.Min(c.counterWindowBase + c.counterWindowPerAgility * Math.Max(0, agility), c.counterWindowMax);
    }
}
