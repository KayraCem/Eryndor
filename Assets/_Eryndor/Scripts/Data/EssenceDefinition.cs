// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using System.Collections.Generic;
using Eryndor.Core.Essence;
using Eryndor.Core.Items;
using Eryndor.Core.Validation;
using UnityEngine;

namespace Eryndor.Data
{
    /// <summary>Canavardan düşen Essence'ın statik tanımı (GDD 4.5).</summary>
    [CreateAssetMenu(menuName = "Eryndor/Essence", fileName = "Essence_")]
    public sealed class EssenceDefinition : DefinitionBase
    {
        [SerializeField] private Rarity rarity;
        [SerializeField] private Sprite icon;
        [SerializeField] private List<EssenceBonus> bonuses = new List<EssenceBonus>();

        private EssenceData _cached;

        public Rarity Rarity => rarity;
        public Sprite Icon => icon;
        public IReadOnlyList<EssenceBonus> Bonuses => bonuses;

        /// <summary>Çekirdek koda verilecek değişmez kopya.</summary>
        public EssenceData ToData() =>
            _cached ??= new EssenceData(Id, DisplayName, rarity, bonuses.ToArray());

        protected override void ValidateFields(ValidationResult r)
        {
            r.Require(bonuses.Count > 0, "Essence hiçbir bonus vermiyor.");
            var seen = new HashSet<EssenceBonusType>();
            foreach (var b in bonuses)
            {
                r.Recommend(seen.Add(b.type), $"{b.type} bonusu iki kez tanımlı.");
                r.Require(b.value > 0f, $"{b.type} bonusunun değeri 0 ya da negatif.");
            }
        }

        protected override void OnValidate()
        {
            _cached = null; // Inspector'da değer değişirse kopyayı yenile
            base.OnValidate();
        }

#if UNITY_EDITOR
        public void EditorSet(Rarity r, params EssenceBonus[] b)
        {
            rarity = r;
            bonuses = new List<EssenceBonus>(b);
            _cached = null;
        }
#endif
    }
}
