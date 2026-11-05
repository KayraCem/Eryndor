// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using System.Collections.Generic;
using Eryndor.Core.Combat;
using Eryndor.Core.Stats;
using Eryndor.Core.Validation;
using UnityEngine;

namespace Eryndor.Data
{
    /// <summary>Oynanabilir karakterin (Kael, Dövüş Sanatçısı) statik tanımı.</summary>
    [CreateAssetMenu(menuName = "Eryndor/Character (Player)", fileName = "Character_")]
    public sealed class CharacterDefinition : DefinitionBase
    {
        [SerializeField] private StatBlock baseStats = new StatBlock(10, 10, 10);
        [SerializeField, Range(0, 6)] private int essenceSlots = 3;
        [SerializeField, Range(1, 8)] private int inventorySlots = 4;
        [SerializeField, Min(0f)] private float moveSpeed = 5f;
        [SerializeField, Min(0f)] private float dodgeDistance = 4f;
        [SerializeField, Min(0.05f)] private float dodgeDuration = 0.4f;

        [Header("Silahsız dövüş")]
        [Tooltip("3'lü kombonun vuruşları; son vuruş kombo çarpanını alır.")]
        [SerializeField] private AttackData lightAttack = new AttackData { id = "light", displayName = "Hafif vuruş", baseDamage = 8f, windupTime = 0.15f, recoveryTime = 0.2f, maxRange = 1.8f };
        [SerializeField] private AttackData heavyAttack = new AttackData { id = "heavy", displayName = "Ağır vuruş", baseDamage = 20f, windupTime = 0.5f, recoveryTime = 0.4f, maxRange = 2f };
        [Tooltip("Başarılı counter'ın karşı vuruşu.")]
        [SerializeField] private AttackData counterStrike = new AttackData { id = "counter", displayName = "Karşı vuruş", baseDamage = 12f, windupTime = 0.1f, recoveryTime = 0.2f, maxRange = 2.5f };

        [Header("Teknikler (en fazla 3)")]
        [SerializeField] private List<AbilityDefinition> techniques = new List<AbilityDefinition>();

        public StatBlock BaseStats => baseStats;
        public int EssenceSlots => essenceSlots;
        public int InventorySlots => inventorySlots;
        public float MoveSpeed => moveSpeed;
        public float DodgeDistance => dodgeDistance;
        public float DodgeDuration => dodgeDuration;
        public AttackData LightAttack => lightAttack;
        public AttackData HeavyAttack => heavyAttack;
        public AttackData CounterStrike => counterStrike;
        public IReadOnlyList<AbilityDefinition> Techniques => techniques;

        protected override void ValidateFields(ValidationResult r)
        {
            r.Require(baseStats.strength >= 0 && baseStats.agility >= 0 && baseStats.vitality >= 0, "Statlar negatif olamaz.");
            r.Require(moveSpeed > 0f, "Hareket hızı 0.");
            r.Merge(lightAttack.Validate(), "Hafif vuruş: ");
            r.Merge(heavyAttack.Validate(), "Ağır vuruş: ");
            r.Merge(counterStrike.Validate(), "Karşı vuruş: ");
            r.Require(techniques.Count <= 3, "En fazla 3 teknik (Q, E ve bir yedek).");
            foreach (var t in techniques) r.Require(t != null, "Teknik listesinde boş eleman var.");
        }

#if UNITY_EDITOR
        public void EditorSet(StatBlock stats, params AbilityDefinition[] techs)
        {
            baseStats = stats; techniques = new List<AbilityDefinition>(techs);
        }
#endif
    }
}
