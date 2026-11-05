// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using System.Collections.Generic;
using Eryndor.Core.Combat;
using Eryndor.Core.Stats;
using Eryndor.Core.Validation;
using UnityEngine;

namespace Eryndor.Data
{
    /// <summary>Bir canavarın ya da boss'un statik tanımı (GDD 7, düşman listesi).</summary>
    [CreateAssetMenu(menuName = "Eryndor/Enemy", fileName = "Enemy_")]
    public sealed class EnemyDefinition : DefinitionBase
    {
        [Header("Statlar")]
        [SerializeField] private StatBlock stats = new StatBlock(5, 5, 0);
        [Tooltip("Can doğrudan verilir; VIT formülünü kullanmak için 0 bırak.")]
        [SerializeField, Min(0f)] private float maxHpOverride = 80f;
        [SerializeField, Min(0f)] private float defense;

        [Header("Hareket ve algı")]
        [SerializeField, Min(0f)] private float patrolSpeed = 1.5f;
        [SerializeField, Min(0f)] private float chaseSpeed = 3.5f;
        [SerializeField, Min(0f)] private float sightRange = 12f;
        [SerializeField, Range(10f, 360f)] private float sightAngle = 110f;
        [SerializeField, Min(0f)] private float hearingRange = 5f;
        [Tooltip("Şüphe durumunda oyuncuyu fark etmek için gereken süre (sn).")]
        [SerializeField, Min(0f)] private float suspicionTime = 0.5f;

        [Header("Saldırılar")]
        [SerializeField] private List<AttackData> attacks = new List<AttackData>();

        [Header("Ödüller")]
        [SerializeField] private EssenceDefinition essenceDrop;
        [SerializeField, Range(0f, 1f)] private float essenceDropChance = 0.25f;
        [SerializeField] private LootTableDefinition lootTable;
        [SerializeField] private BarkSetDefinition barks;

        [Header("Boss")]
        [SerializeField] private bool isBoss;
        [Tooltip("Can bu oranın altına düşünce Öfke Fazı (0 = faz yok).")]
        [SerializeField, Range(0f, 1f)] private float rageThreshold;
        [SerializeField, Min(1f)] private float rageSpeedMultiplier = 1.3f;

        public StatBlock Stats => stats;
        public float MaxHpOverride => maxHpOverride;
        public float Defense => defense;
        public float PatrolSpeed => patrolSpeed;
        public float ChaseSpeed => chaseSpeed;
        public float SightRange => sightRange;
        public float SightAngle => sightAngle;
        public float HearingRange => hearingRange;
        public float SuspicionTime => suspicionTime;
        public IReadOnlyList<AttackData> Attacks => attacks;
        public EssenceDefinition EssenceDrop => essenceDrop;
        public float EssenceDropChance => essenceDropChance;
        public LootTableDefinition LootTable => lootTable;
        public BarkSetDefinition Barks => barks;
        public bool IsBoss => isBoss;
        public float RageThreshold => rageThreshold;
        public float RageSpeedMultiplier => rageSpeedMultiplier;

        /// <summary>En uzun menzilli saldırının menzili; kovalama bu mesafede durur.</summary>
        public float MaxAttackRange
        {
            get { float m = 0f; foreach (var a in attacks) if (a != null && a.maxRange > m) m = a.maxRange; return m; }
        }

        protected override void ValidateFields(ValidationResult r)
        {
            r.Require(attacks.Count > 0, "Hiç saldırı tanımlı değil.");
            foreach (var a in attacks)
            {
                if (a == null) { r.Error("Saldırı listesinde boş eleman var."); continue; }
                r.Merge(a.Validate(), $"Saldırı '{a.id}': ");
            }
            r.Require(chaseSpeed >= patrolSpeed, "Kovalama hızı devriye hızından düşük.");
            r.Recommend(essenceDrop != null || essenceDropChance == 0f, "Essence şansı var ama Essence atanmamış.");
            if (isBoss)
            {
                r.Recommend(essenceDropChance >= 1f, "Boss Essence'ı garanti düşmeli (GDD: %100).");
                bool hasCounterable = false;
                foreach (var a in attacks) if (a != null && a.counterable) hasCounterable = true;
                r.Require(hasCounterable, "Boss'un en az bir counter'lanabilir saldırısı olmalı; yoksa ana mekanik işlemez.");
            }
        }

#if UNITY_EDITOR
        public void EditorSet(StatBlock s, float hp, float def, float patrol, float chase,
                              EssenceDefinition essence, float essenceChance, LootTableDefinition loot,
                              BarkSetDefinition barkSet, bool boss, float rage, params AttackData[] attackList)
        {
            stats = s; maxHpOverride = hp; defense = def; patrolSpeed = patrol; chaseSpeed = chase;
            essenceDrop = essence; essenceDropChance = essenceChance; lootTable = loot; barks = barkSet;
            isBoss = boss; rageThreshold = rage; attacks = new List<AttackData>(attackList);
        }
#endif
    }
}
