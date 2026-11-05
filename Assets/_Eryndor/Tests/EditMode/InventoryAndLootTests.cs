// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using System.Collections.Generic;
using Eryndor.Core.Characters;
using Eryndor.Core.Combat;
using Eryndor.Core.Items;
using Eryndor.Core.Stats;
using NUnit.Framework;

namespace Eryndor.Tests
{
    public class InventoryAndLootTests
    {
        private static ItemData Potion => new ItemData("heal", "Şifa", Rarity.Common, 3, ConsumableEffect.Heal, 40f);
        private static ItemData Smoke => new ItemData("smoke", "Duman", Rarity.Rare, 2, ConsumableEffect.Smoke, 3f);

        [Test]
        public void Add_StacksUpToMax_ThenUsesNewSlot()
        {
            var inv = new Inventory(4);
            Assert.AreEqual(0, inv.Add(Potion, 5));
            Assert.AreEqual(3, inv.GetSlot(0).Count);
            Assert.AreEqual(2, inv.GetSlot(1).Count);
            Assert.AreEqual(5, inv.CountOf("heal"));
        }

        [Test]
        public void Add_ReturnsLeftover_WhenFull()
        {
            var inv = new Inventory(1);
            Assert.AreEqual(2, inv.Add(Potion, 5));
        }

        [Test]
        public void ConsumeOne_EmptiesSlot()
        {
            var inv = new Inventory(4);
            inv.Add(Potion, 1);
            Assert.IsNotNull(inv.ConsumeOne(0));
            Assert.IsTrue(inv.GetSlot(0).IsEmpty);
            Assert.IsNull(inv.ConsumeOne(0));
        }

        [Test]
        public void Potion_Heals_SmokeIsLeftToRuntime()
        {
            var c = new CharacterState(new StatBlock(0, 0, 0), new CombatConfig());
            c.TakeDamage(60f, 0f);
            Assert.IsTrue(Potion.ApplyTo(c));
            Assert.AreEqual(80f, c.CurrentHp);
            Assert.IsFalse(Smoke.ApplyTo(c));
        }

        private static readonly List<LootEntry<string>> Table = new List<LootEntry<string>>
        {
            new LootEntry<string>("potion", 30f),
            new LootEntry<string>("crystal", 20f),
            new LootEntry<string>("never", 0f)
        };

        // Toplam ağırlık = 30 + 20 + 50 (hiçbir şey) = 100
        [TestCase(0.10f, "potion")]
        [TestCase(0.35f, "crystal")]
        [TestCase(0.80f, null)]
        public void Roll_PicksByWeight(float roll, string expected)
        {
            var drops = LootRoller.Roll(Table, 1, 50f, new FakeRandom(roll));
            if (expected == null) Assert.AreEqual(0, drops.Count);
            else { Assert.AreEqual(1, drops.Count); Assert.AreEqual(expected, drops[0].Item); }
        }

        [Test]
        public void Roll_CountWithinRange()
        {
            var table = new List<LootEntry<string>> { new LootEntry<string>("potion", 1f, 1, 3) };
            var rng = new FakeRandom(0f) { IntToReturn = 2 };
            var drops = LootRoller.Roll(table, 1, 0f, rng);
            Assert.AreEqual(2, drops[0].Count);
            Assert.AreEqual(1, rng.LastIntMin);
            Assert.AreEqual(4, rng.LastIntMax); // üst sınır dahil olsun diye +1
        }

        [Test]
        public void Roll_ZeroWeightEntry_NeverDrops()
        {
            var table = new List<LootEntry<string>> { new LootEntry<string>("never", 0f) };
            Assert.AreEqual(0, LootRoller.Roll(table, 10, 0f, new FakeRandom(0f, 0.5f, 0.99f)).Count);
        }

        [Test]
        public void RollChance_RespectsProbability()
        {
            Assert.IsTrue(LootRoller.RollChance(0.25f, new FakeRandom(0.2f)));
            Assert.IsFalse(LootRoller.RollChance(0.25f, new FakeRandom(0.3f)));
            Assert.IsFalse(LootRoller.RollChance(0f, new FakeRandom(0f)));
        }
    }
}
