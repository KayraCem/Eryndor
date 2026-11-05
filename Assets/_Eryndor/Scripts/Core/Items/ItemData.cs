// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using System;
using Eryndor.Core.Characters;

namespace Eryndor.Core.Items
{
    public enum ConsumableEffect
    {
        Heal,          // Şifa iksiri
        RestoreMana,   // Mana kristali
        Smoke          // Duman bombası: etkisini Unity tarafı uygular (düşmanlar oyuncuyu kaybeder)
    }

    /// <summary>Tüketilebilir bir eşyanın çekirdekteki statik tanımı.</summary>
    public sealed class ItemData
    {
        public string Id { get; }
        public string DisplayName { get; }
        public Rarity Rarity { get; }
        public int MaxStack { get; }
        public ConsumableEffect Effect { get; }
        public float Amount { get; }

        public ItemData(string id, string displayName, Rarity rarity, int maxStack, ConsumableEffect effect, float amount)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            DisplayName = displayName ?? id;
            Rarity = rarity;
            MaxStack = Math.Max(1, maxStack);
            Effect = effect;
            Amount = amount;
        }

        /// <summary>
        /// Can ve Mana etkilerini doğrudan uygular. Duman gibi dünyaya bağlı etkiler için false döner;
        /// onları Unity tarafı uygular.
        /// </summary>
        public bool ApplyTo(CharacterState target)
        {
            if (target == null) return false;
            switch (Effect)
            {
                case ConsumableEffect.Heal: target.Heal(Amount); return true;
                case ConsumableEffect.RestoreMana: target.RestoreMana(Amount); return true;
                default: return false;
            }
        }
    }
}
