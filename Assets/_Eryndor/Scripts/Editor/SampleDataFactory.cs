// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using System.IO;
using Eryndor.Core.Barks;
using Eryndor.Core.Combat;
using Eryndor.Core.Essence;
using Eryndor.Core.Items;
using Eryndor.Core.Stats;
using Eryndor.Data;
using UnityEditor;
using UnityEngine;

namespace Eryndor.EditorTools
{
    /// <summary>
    /// Menü: Eryndor > Örnek Verileri Oluştur. GDD'deki örnek değerlerle tüm ScriptableObject'leri
    /// Assets/_Eryndor/Data altına üretir. Var olan dosyaların üzerine yazmaz.
    /// </summary>
    public static class SampleDataFactory
    {
        private const string DataRoot = "Assets/_Eryndor/Data";

        [MenuItem("Eryndor/Örnek Verileri Oluştur")]
        public static void CreateAll()
        {
            Directory.CreateDirectory(DataRoot);

            // Dengeleme
            Create<CombatTuningAsset>("CombatTuning", _ => { });

            // Eşyalar (GDD 4.6)
            var potion = Create<ItemDefinition>("Item_SifaIksiri", i =>
            {
                i.EditorSetIdentity("item_heal", "Şifa İksiri", "+40 can.");
                i.EditorSet(Rarity.Common, ConsumableEffect.Heal, 40f, 3);
            });
            var crystal = Create<ItemDefinition>("Item_ManaKristali", i =>
            {
                i.EditorSetIdentity("item_mana", "Mana Kristali", "+30 Mana.");
                i.EditorSet(Rarity.Common, ConsumableEffect.RestoreMana, 30f, 3);
            });
            var smoke = Create<ItemDefinition>("Item_DumanBombasi", i =>
            {
                i.EditorSetIdentity("item_smoke", "Duman Bombası", "Düşmanlar 3 sn boyunca seni kaybeder.");
                i.EditorSet(Rarity.Rare, ConsumableEffect.Smoke, 3f, 2);
            });

            // Loot tabloları
            var commonLoot = Create<LootTableDefinition>("Loot_Yaygin", l =>
            {
                l.EditorSetIdentity("loot_common", "Yaygın canavar ganimeti", "");
                l.EditorSet(1, 50f,
                    new ItemLootRow { item = potion, weight = 30f, minCount = 1, maxCount = 1 },
                    new ItemLootRow { item = crystal, weight = 20f, minCount = 1, maxCount = 1 });
            });
            var bossLoot = Create<LootTableDefinition>("Loot_Bekci", l =>
            {
                l.EditorSetIdentity("loot_boss", "Bekçi ganimeti", "");
                l.EditorSet(2, 0f,
                    new ItemLootRow { item = potion, weight = 50f, minCount = 1, maxCount = 2 },
                    new ItemLootRow { item = smoke, weight = 50f, minCount = 1, maxCount = 1 });
            });

            // Essence'lar (GDD 7)
            var wolfEssence = Create<EssenceDefinition>("Essence_MagaraKurdu", e =>
            {
                e.EditorSetIdentity("essence_wolf", "Mağara Kurdu Essence'ı", "+%10 hareket hızı.");
                e.EditorSet(Rarity.Common, new EssenceBonus(EssenceBonusType.MoveSpeedPercent, 10f));
            });
            var boneEssence = Create<EssenceDefinition>("Essence_KemikAskeri", e =>
            {
                e.EditorSetIdentity("essence_bone", "Kemik Askeri Essence'ı", "+%15 counter hasarı.");
                e.EditorSet(Rarity.Common, new EssenceBonus(EssenceBonusType.CounterDamagePercent, 15f));
            });
            Create<EssenceDefinition>("Essence_ManaSinekleri", e =>
            {
                e.EditorSetIdentity("essence_flies", "Mana Sinekleri Essence'ı", "+%20 Mana kazanımı.");
                e.EditorSet(Rarity.Rare, new EssenceBonus(EssenceBonusType.ManaGainPercent, 20f));
            });
            var heartEssence = Create<EssenceDefinition>("Essence_BekciKalbi", e =>
            {
                e.EditorSetIdentity("essence_warden_heart", "Bekçi Kalbi", "Counter sonrası kısa dokunulmazlık, +3 VIT.");
                e.EditorSet(Rarity.Legendary,
                    new EssenceBonus(EssenceBonusType.PostCounterInvulnerabilitySeconds, 0.5f),
                    new EssenceBonus(EssenceBonusType.FlatVitality, 3f));
            });

            // Teknikler (GDD 4.4)
            var ironPalm = Create<AbilityDefinition>("Ability_DemirAvuc", a =>
            {
                a.EditorSetIdentity("tech_iron_palm", "Demir Avuç", "Öne atılan güçlü vuruş, savunmayı kırar.");
                a.EditorSet(TechniqueKind.DashStrike, 30f, 45f, 3f, 0f, 1f, true);
            });
            var waterStance = Create<AbilityDefinition>("Ability_SuDurusu", a =>
            {
                a.EditorSetIdentity("tech_water_stance", "Su Duruşu", "2 sn boyunca counter penceresi iki katı.");
                a.EditorSet(TechniqueKind.WaterStance, 20f, 0f, 0f, 2f, 2f, false);
            });
            Create<AbilityDefinition>("Ability_YediRuzgar", a =>
            {
                a.EditorSetIdentity("tech_seven_winds", "Yedi Rüzgar Tekmesi", "Etraftaki herkese hasar.");
                a.EditorSet(TechniqueKind.AreaKick, 45f, 25f, 3f, 0f, 1f, false);
            });

            // Oyuncu
            Create<CharacterDefinition>("Character_Kael", c =>
            {
                c.EditorSetIdentity("player_kael", "Kael", "Sessiz Avuç Tarikatı'nın son öğrencisi.");
                c.EditorSet(new StatBlock(10, 10, 10), ironPalm, waterStance);
            });

            // Barklar (GDD 7)
            var boneBarks = Create<BarkSetDefinition>("Barks_KemikAskeri", b =>
            {
                b.EditorSetIdentity("barks_bone", "Kemik Askeri", "");
                b.EditorSet(5f,
                    new BarkLine(BarkTrigger.PlayerSpotted, "Yeni et... yeni kemik."),
                    new BarkLine(BarkTrigger.PlayerSpotted, "Kuyu bir tane daha gönderdi."),
                    new BarkLine(BarkTrigger.Countered, "Ne...?"),
                    new BarkLine(BarkTrigger.Death, "Toz... olurum..."));
            });
            var gorrathBarks = Create<BarkSetDefinition>("Barks_Gorrath", b =>
            {
                b.EditorSetIdentity("barks_gorrath", "Gorrath", "");
                b.EditorSet(4f,
                    new BarkLine(BarkTrigger.BattleStart, "Silahsız mı geldin? Tarikatın aptallığı ölmemiş."),
                    new BarkLine(BarkTrigger.Countered, "İmkansız!"),
                    new BarkLine(BarkTrigger.PlayerRepeatsCounter, "Hep aynı numara."),
                    new BarkLine(BarkTrigger.PhaseChange, "Yeter! Kuyu seni yutacak!"),
                    new BarkLine(BarkTrigger.LowHealth, "Bu kat... benim..."),
                    new BarkLine(BarkTrigger.Death, "Usta... seni... iyi yetiştirmiş..."));
            });

            // Düşmanlar (GDD 7)
            Create<EnemyDefinition>("Enemy_KemikAskeri", e =>
            {
                e.EditorSetIdentity("enemy_bone_soldier", "Kemik Askeri", "Yavaş, uzun uyarılı; counter öğretir.");
                e.EditorSet(new StatBlock(4, 2, 0), 80f, 2f, 1.2f, 2.8f, boneEssence, 0.25f, commonLoot, boneBarks, false, 0f,
                    new AttackData { id = "bone_slash", displayName = "Kemik Kesiği", baseDamage = 14f, windupTime = 0.9f, recoveryTime = 0.8f, maxRange = 2f, counterable = true });
            });
            Create<EnemyDefinition>("Enemy_MagaraKurdu", e =>
            {
                e.EditorSetIdentity("enemy_cave_wolf", "Mağara Kurdu", "Hızlı, grup halinde.");
                e.EditorSet(new StatBlock(2, 8, 0), 40f, 0f, 2f, 5f, wolfEssence, 0.3f, commonLoot, null, false, 0f,
                    new AttackData { id = "wolf_bite", displayName = "Isırık", baseDamage = 6f, windupTime = 0.45f, recoveryTime = 0.5f, maxRange = 1.6f, counterable = true });
            });
            Create<EnemyDefinition>("Enemy_Gorrath", e =>
            {
                e.EditorSetIdentity("boss_gorrath", "Kat Bekçisi Gorrath", "İlk Kat'ın bekçisi.");
                e.EditorSet(new StatBlock(8, 4, 0), 600f, 3f, 2f, 3.2f, heartEssence, 1f, bossLoot, gorrathBarks, true, 0.5f,
                    new AttackData { id = "slam", displayName = "Ezici Darbe", baseDamage = 25f, windupTime = 0.8f, recoveryTime = 0.9f, maxRange = 3f, counterable = true, weight = 3f },
                    new AttackData { id = "combo", displayName = "Üçlü Vuruş", baseDamage = 18f, windupTime = 0.6f, recoveryTime = 0.7f, maxRange = 2.5f, counterable = true, weight = 2f },
                    new AttackData { id = "lunge", displayName = "Atılma", baseDamage = 22f, windupTime = 0.7f, recoveryTime = 1f, minRange = 3f, maxRange = 8f, counterable = true, weight = 2f },
                    new AttackData { id = "essence_wave", displayName = "Essence Dalgası", baseDamage = 20f, windupTime = 1f, recoveryTime = 1.2f, maxRange = 6f, counterable = false, isArea = true, weight = 1f });
            });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Eryndor] Örnek veriler oluşturuldu: " + DataRoot);
            DataValidatorMenu.ValidateAll();
        }

        /// <summary>Asset yoksa oluşturur ve doldurur; varsa olduğu gibi döner.</summary>
        private static T Create<T>(string fileName, System.Action<T> fill) where T : ScriptableObject
        {
            string path = $"{DataRoot}/{fileName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;

            var asset = ScriptableObject.CreateInstance<T>();
            fill(asset);
            AssetDatabase.CreateAsset(asset, path);
            EditorUtility.SetDirty(asset);
            return asset;
        }
    }
}
