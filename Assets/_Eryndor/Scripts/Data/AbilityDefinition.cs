// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using Eryndor.Core.Validation;
using UnityEngine;

namespace Eryndor.Data
{
    public enum TechniqueKind
    {
        DashStrike,   // Demir Avuç: öne atılan güçlü vuruş
        AreaKick,     // Yedi Rüzgar Tekmesi: etraftaki herkese hasar
        WaterStance   // Su Duruşu: counter penceresi geçici olarak büyür
    }

    /// <summary>Mana harcayan bir Dövüş Sanatçısı tekniği (GDD 4.4).</summary>
    [CreateAssetMenu(menuName = "Eryndor/Ability (Technique)", fileName = "Ability_")]
    public sealed class AbilityDefinition : DefinitionBase
    {
        [SerializeField] private TechniqueKind kind;
        [SerializeField] private Sprite icon;
        [SerializeField, Min(0f)] private float manaCost = 30f;
        [SerializeField, Min(0f)] private float baseDamage = 45f;
        [Tooltip("DashStrike: atılma mesafesi; AreaKick: etki yarıçapı (metre).")]
        [SerializeField, Min(0f)] private float range = 3f;
        [Tooltip("WaterStance: etki süresi (sn).")]
        [SerializeField, Min(0f)] private float duration;
        [Tooltip("WaterStance: counter penceresi çarpanı.")]
        [SerializeField, Min(1f)] private float counterWindowMultiplier = 1f;
        [SerializeField] private bool breaksShield;
        [SerializeField, Min(0f)] private float cooldown = 1f;

        public TechniqueKind Kind => kind;
        public Sprite Icon => icon;
        public float ManaCost => manaCost;
        public float BaseDamage => baseDamage;
        public float Range => range;
        public float Duration => duration;
        public float CounterWindowMultiplier => counterWindowMultiplier;
        public bool BreaksShield => breaksShield;
        public float Cooldown => cooldown;

        protected override void ValidateFields(ValidationResult r)
        {
            r.Require(manaCost <= 100f, "Mana maliyeti maksimum Mana'dan (100) büyük.");
            if (kind == TechniqueKind.WaterStance)
            {
                r.Require(duration > 0f, "Su Duruşu için süre 0'dan büyük olmalı.");
                r.Require(counterWindowMultiplier > 1f, "Su Duruşu counter penceresini büyütmeli (> 1).");
            }
            else
            {
                r.Require(baseDamage > 0f, "Saldırı tekniğinin hasarı 0.");
                r.Require(range > 0f, "Menzil/yarıçap 0.");
            }
        }

#if UNITY_EDITOR
        public void EditorSet(TechniqueKind k, float mana, float damage, float rng, float dur, float windowMult, bool shield)
        {
            kind = k; manaCost = mana; baseDamage = damage; range = rng;
            duration = dur; counterWindowMultiplier = windowMult; breaksShield = shield;
        }
#endif
    }
}
