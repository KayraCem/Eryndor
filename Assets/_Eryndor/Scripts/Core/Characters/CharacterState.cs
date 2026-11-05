// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using System;
using Eryndor.Core.Combat;
using Eryndor.Core.Essence;
using Eryndor.Core.Stats;

namespace Eryndor.Core.Characters
{
    /// <summary>
    /// Bir karakterin oyun sırasında değişen durumu: can, Mana, Stamina, Essence slotları.
    /// Statik tanım (CharacterDefinition / EnemyDefinition) değişmez; değişen her şey burada.
    /// </summary>
    public sealed class CharacterState
    {
        private readonly CombatConfig _config;
        private readonly float _maxHpOverride;
        private float _invulnerableUntil = float.NegativeInfinity;

        public StatBlock BaseStats { get; }
        public EssenceSlots Essences { get; }
        public float Defense { get; }

        public float CurrentHp { get; private set; }
        public float CurrentMana { get; private set; }
        public float CurrentStamina { get; private set; }

        public StatBlock EffectiveStats => BaseStats + Essences.FlatStatBonus();
        /// <summary>maxHpOverride > 0 ise o kullanılır (düşmanlar), yoksa VIT formülü (oyuncu).</summary>
        public float MaxHp => _maxHpOverride > 0f
            ? _maxHpOverride
            : StatFormulas.MaxHp(_config, EffectiveStats.vitality);
        public float MaxMana => _config.maxMana;
        public float MaxStamina => _config.maxStamina;
        public bool IsDead => CurrentHp <= 0f;

        /// <summary>(mevcut, maksimum)</summary>
        public event Action<float, float> HealthChanged;
        public event Action<float, float> ManaChanged;
        public event Action<float, float> StaminaChanged;
        public event Action Died;

        public CharacterState(StatBlock baseStats, CombatConfig config, int essenceSlots = 0,
                              float defense = 0f, float maxHpOverride = 0f)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _maxHpOverride = maxHpOverride;
            BaseStats = baseStats;
            Defense = defense;
            Essences = new EssenceSlots(essenceSlots);
            Essences.Changed += ClampHpToMax;

            CurrentHp = MaxHp;
            CurrentMana = 0f;               // GDD: Mana savaşta dolar, başta boştur
            CurrentStamina = MaxStamina;
        }

        public bool IsInvulnerable(float now) => now < _invulnerableUntil;

        public void SetInvulnerableUntil(float time)
        {
            if (time > _invulnerableUntil) _invulnerableUntil = time;
        }

        /// <summary>Hasar uygular. Gerçekte verilen hasarı döner.</summary>
        public float TakeDamage(float amount, float now)
        {
            if (IsDead || amount <= 0f || IsInvulnerable(now)) return 0f;
            float applied = Math.Min(amount, CurrentHp);
            CurrentHp -= applied;
            HealthChanged?.Invoke(CurrentHp, MaxHp);
            if (CurrentHp <= 0f)
            {
                CurrentHp = 0f;
                Died?.Invoke();
            }
            return applied;
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;
            CurrentHp = Math.Min(MaxHp, CurrentHp + amount);
            HealthChanged?.Invoke(CurrentHp, MaxHp);
        }

        /// <summary>Mana kazanır; Essence'tan gelen ManaGainPercent bonusu uygulanır.</summary>
        public void GainMana(float amount)
        {
            if (amount <= 0f) return;
            float bonus = 1f + Essences.Total(EssenceBonusType.ManaGainPercent) / 100f;
            CurrentMana = Math.Min(MaxMana, CurrentMana + amount * bonus);
            ManaChanged?.Invoke(CurrentMana, MaxMana);
        }

        /// <summary>Bonus uygulanmadan doğrudan Mana ekler (ör. Mana kristali).</summary>
        public void RestoreMana(float amount)
        {
            if (amount <= 0f) return;
            CurrentMana = Math.Min(MaxMana, CurrentMana + amount);
            ManaChanged?.Invoke(CurrentMana, MaxMana);
        }

        public bool TrySpendMana(float cost)
        {
            if (cost < 0f || CurrentMana < cost) return false;
            CurrentMana -= cost;
            ManaChanged?.Invoke(CurrentMana, MaxMana);
            return true;
        }

        public bool TrySpendStamina(float cost)
        {
            if (cost < 0f || CurrentStamina < cost) return false;
            CurrentStamina -= cost;
            StaminaChanged?.Invoke(CurrentStamina, MaxStamina);
            return true;
        }

        public void RegenerateStamina(float deltaTime)
        {
            if (CurrentStamina >= MaxStamina || deltaTime <= 0f) return;
            CurrentStamina = Math.Min(MaxStamina, CurrentStamina + _config.staminaRegenPerSecond * deltaTime);
            StaminaChanged?.Invoke(CurrentStamina, MaxStamina);
        }

        private void ClampHpToMax()
        {
            if (CurrentHp > MaxHp) CurrentHp = MaxHp;
            HealthChanged?.Invoke(CurrentHp, MaxHp);
        }
    }
}
