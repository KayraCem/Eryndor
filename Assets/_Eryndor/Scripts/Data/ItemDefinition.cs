// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using Eryndor.Core.Items;
using Eryndor.Core.Validation;
using UnityEngine;

namespace Eryndor.Data
{
    /// <summary>Tüketilebilir eşya tanımı (GDD 4.6).</summary>
    [CreateAssetMenu(menuName = "Eryndor/Item (Consumable)", fileName = "Item_")]
    public sealed class ItemDefinition : DefinitionBase
    {
        [SerializeField] private Rarity rarity;
        [SerializeField] private Sprite icon;
        [SerializeField] private ConsumableEffect effect;
        [Tooltip("Heal: can, RestoreMana: Mana, Smoke: süre (sn).")]
        [SerializeField, Min(0f)] private float amount = 40f;
        [SerializeField, Min(1)] private int maxStack = 3;

        private ItemData _cached;

        public Rarity Rarity => rarity;
        public Sprite Icon => icon;
        public ConsumableEffect Effect => effect;
        public float Amount => amount;

        public ItemData ToData() =>
            _cached ??= new ItemData(Id, DisplayName, rarity, maxStack, effect, amount);

        protected override void ValidateFields(ValidationResult r)
        {
            r.Require(amount > 0f, "Etki miktarı 0.");
            r.Require(maxStack >= 1, "maxStack en az 1 olmalı.");
        }

        protected override void OnValidate()
        {
            _cached = null;
            base.OnValidate();
        }

#if UNITY_EDITOR
        public void EditorSet(Rarity r, ConsumableEffect e, float amt, int stack)
        {
            rarity = r; effect = e; amount = amt; maxStack = stack; _cached = null;
        }
#endif
    }
}
