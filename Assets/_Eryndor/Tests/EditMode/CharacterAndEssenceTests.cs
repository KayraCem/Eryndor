// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using Eryndor.Core.Characters;
using Eryndor.Core.Combat;
using Eryndor.Core.Essence;
using Eryndor.Core.Items;
using Eryndor.Core.Stats;
using NUnit.Framework;

namespace Eryndor.Tests
{
    public class CharacterAndEssenceTests
    {
        private CombatConfig _config;

        [SetUp] public void SetUp() => _config = new CombatConfig();

        private static EssenceData Essence(string id, EssenceBonusType type, float value) =>
            new EssenceData(id, id, Rarity.Common, new[] { new EssenceBonus(type, value) });

        [Test]
        public void NewCharacter_HasFullHp_ZeroMana_FullStamina()
        {
            var c = new CharacterState(new StatBlock(10, 10, 10), _config, 3);
            Assert.AreEqual(200f, c.CurrentHp);
            Assert.AreEqual(0f, c.CurrentMana);
            Assert.AreEqual(100f, c.CurrentStamina);
        }

        [Test]
        public void MaxHpOverride_IsUsedForEnemies()
        {
            var c = new CharacterState(new StatBlock(4, 2, 0), _config, 0, 2f, 80f);
            Assert.AreEqual(80f, c.MaxHp);
            Assert.AreEqual(80f, c.CurrentHp);
        }

        [Test]
        public void Death_FiresOnce()
        {
            var c = new CharacterState(new StatBlock(0, 0, 0), _config);
            int deaths = 0;
            c.Died += () => deaths++;
            c.TakeDamage(150f, 0f);
            c.TakeDamage(10f, 0f);
            Assert.IsTrue(c.IsDead);
            Assert.AreEqual(1, deaths);
        }

        [Test]
        public void Invulnerable_TakesNoDamage()
        {
            var c = new CharacterState(new StatBlock(0, 0, 0), _config);
            c.SetInvulnerableUntil(5f);
            Assert.AreEqual(0f, c.TakeDamage(50f, 4.9f));
            Assert.AreEqual(50f, c.TakeDamage(50f, 5f));
        }

        [Test]
        public void ManaGain_UsesEssenceBonus_AndCapsAtMax()
        {
            var c = new CharacterState(new StatBlock(0, 0, 0), _config, 3);
            c.Essences.TryAbsorb(Essence("flies", EssenceBonusType.ManaGainPercent, 20f));
            c.GainMana(15f);
            Assert.AreEqual(18f, c.CurrentMana, 0.001f);
            c.GainMana(1000f);
            Assert.AreEqual(100f, c.CurrentMana);
        }

        [Test]
        public void SpendMana_FailsWhenNotEnough()
        {
            var c = new CharacterState(new StatBlock(0, 0, 0), _config);
            c.RestoreMana(20f);
            Assert.IsFalse(c.TrySpendMana(30f));
            Assert.AreEqual(20f, c.CurrentMana);
            Assert.IsTrue(c.TrySpendMana(20f));
        }

        [Test]
        public void Slots_FillThenRequireReplaceIndex()
        {
            var slots = new EssenceSlots(3);
            Assert.IsTrue(slots.TryAbsorb(Essence("a", EssenceBonusType.FlatStrength, 1)));
            Assert.IsTrue(slots.TryAbsorb(Essence("b", EssenceBonusType.FlatStrength, 1)));
            Assert.IsTrue(slots.TryAbsorb(Essence("c", EssenceBonusType.FlatStrength, 1)));
            Assert.IsTrue(slots.IsFull);

            Assert.IsFalse(slots.TryAbsorb(Essence("d", EssenceBonusType.FlatStrength, 1)));  // index yok
            Assert.IsTrue(slots.TryAbsorb(Essence("d", EssenceBonusType.FlatStrength, 1), 1, out var replaced));
            Assert.AreEqual("b", replaced.Id);
            Assert.AreEqual("d", slots.Get(1).Id);
        }

        [Test]
        public void Slots_RejectDuplicate()
        {
            var slots = new EssenceSlots(3);
            slots.TryAbsorb(Essence("a", EssenceBonusType.FlatStrength, 1));
            Assert.IsFalse(slots.TryAbsorb(Essence("a", EssenceBonusType.FlatStrength, 1)));
            Assert.AreEqual(1, slots.Count);
        }

        [Test]
        public void Empower_DoublesBonuses_AndTurnsOff()
        {
            var slots = new EssenceSlots(3);
            slots.TryAbsorb(Essence("wolf", EssenceBonusType.MoveSpeedPercent, 10f));
            slots.SetEmpowered(true, 2f);
            Assert.AreEqual(20f, slots.Total(EssenceBonusType.MoveSpeedPercent), 0.001f);
            slots.SetEmpowered(false, 1f);
            Assert.AreEqual(10f, slots.Total(EssenceBonusType.MoveSpeedPercent), 0.001f);
        }

        [Test]
        public void VitalityEssence_RaisesMaxHp()
        {
            var c = new CharacterState(new StatBlock(10, 10, 10), _config, 3);
            c.Essences.TryAbsorb(Essence("heart", EssenceBonusType.FlatVitality, 3f));
            Assert.AreEqual(230f, c.MaxHp, 0.001f);
        }
    }
}
