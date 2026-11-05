// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using System;
using Eryndor.Core.Validation;

namespace Eryndor.Core.Combat
{
    /// <summary>
    /// Dengeleme değerlerinin hepsi burada. Unity'de CombatTuningAsset içinde tutulur,
    /// böylece kod değişmeden Inspector'dan ayarlanır. Varsayılanlar GDD Bölüm 4'teki değerlerdir.
    /// </summary>
    [Serializable]
    public sealed class CombatConfig : IValidatable
    {
        // Can ve hasar
        public float baseHp = 100f;
        public float hpPerVitality = 10f;
        public float damagePerStrength = 0.05f;

        // Counter
        public float counterWindowBase = 0.25f;
        public float counterWindowPerAgility = 0.005f;
        public float counterWindowMax = 0.4f;
        public float counterDamageMultiplier = 2.5f;
        public float counterManaGain = 15f;
        public float counterStunDuration = 1f;
        public float counterWhiffPenalty = 0.6f;

        // Kombo
        public float comboFinisherMultiplier = 1.5f;
        public float manaPerHit = 3f;

        // Kaynaklar
        public float maxMana = 100f;
        public float maxStamina = 100f;
        public float staminaRegenPerSecond = 25f;
        public float dodgeStaminaCost = 20f;
        public float dodgeInvulnerability = 0.2f;

        // Essence Güçlendirme
        public float empowerDuration = 5f;
        public float empowerCooldown = 20f;
        public float empowerMultiplier = 2f;

        public ValidationResult Validate()
        {
            var r = new ValidationResult();
            r.Require(baseHp > 0f, "baseHp 0'dan büyük olmalı.");
            r.Require(hpPerVitality >= 0f, "hpPerVitality negatif olamaz.");
            r.Require(damagePerStrength >= 0f, "damagePerStrength negatif olamaz.");
            r.Require(counterWindowBase > 0f, "counterWindowBase 0'dan büyük olmalı.");
            r.Require(counterWindowMax >= counterWindowBase, "counterWindowMax, counterWindowBase'den küçük olamaz.");
            r.Require(counterDamageMultiplier >= 1f, "counterDamageMultiplier en az 1 olmalı.");
            r.Require(counterWhiffPenalty >= 0f, "counterWhiffPenalty negatif olamaz.");
            r.Require(maxMana > 0f && maxStamina > 0f, "maxMana ve maxStamina 0'dan büyük olmalı.");
            r.Require(dodgeStaminaCost <= maxStamina, "Kaçış maliyeti maksimum Stamina'dan büyük olamaz.");
            r.Require(empowerMultiplier >= 1f, "empowerMultiplier en az 1 olmalı.");
            r.Recommend(counterWindowMax <= 0.6f, "Counter penceresi 0,6 sn'den uzun; mekanik çok kolaylaşabilir.");
            r.Recommend(counterWhiffPenalty >= counterWindowBase, "Boşa counter cezası pencereden kısa; oyuncu counter'ı spamlayabilir.");
            return r;
        }
    }
}
