// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using Eryndor.Core.Combat;
using Eryndor.Core.Stats;
using NUnit.Framework;

namespace Eryndor.Tests
{
    public class StatFormulasTests
    {
        private CombatConfig _config;

        [SetUp] public void SetUp() => _config = new CombatConfig();

        [Test] public void MaxHp_VIT10_Is200() => Assert.AreEqual(200f, StatFormulas.MaxHp(_config, 10), 0.001f);

        [Test] public void MaxHp_NegativeVit_TreatedAsZero() => Assert.AreEqual(100f, StatFormulas.MaxHp(_config, -5), 0.001f);

        [Test] public void DamageMultiplier_STR10_Is1_5() => Assert.AreEqual(1.5f, StatFormulas.DamageMultiplier(_config, 10), 0.001f);

        [TestCase(0, 0.25f)]
        [TestCase(10, 0.30f)]
        [TestCase(30, 0.40f)]
        [TestCase(100, 0.40f)] // üst sınır
        public void CounterWindow_FollowsFormulaAndCap(int agility, float expected) =>
            Assert.AreEqual(expected, StatFormulas.CounterWindow(_config, agility), 0.0001f);

        [Test]
        public void Damage_SubtractsDefense_NeverNegative()
        {
            Assert.AreEqual(12f, DamageCalculator.Compute(_config, 8f, 10, 1f, 0f), 0.001f);  // 8 × 1.5
            Assert.AreEqual(7f, DamageCalculator.Compute(_config, 8f, 10, 1f, 5f), 0.001f);
            Assert.AreEqual(0f, DamageCalculator.Compute(_config, 8f, 10, 1f, 100f), 0.001f);
        }

        [Test]
        public void CounterDamage_AppliesMultiplierAndEssenceBonus()
        {
            // 12 × 1.5 (STR 10) × 2.5 × 1.15 = 51.75
            Assert.AreEqual(51.75f, DamageCalculator.CounterDamage(_config, 12f, 10, 15f), 0.01f);
        }

        [Test] public void DefaultConfig_IsValid() => Assert.IsTrue(_config.Validate().IsValid, _config.Validate().ToString());

        [Test]
        public void Config_WithMaxBelowBase_IsInvalid()
        {
            _config.counterWindowMax = 0.1f;
            Assert.IsFalse(_config.Validate().IsValid);
        }
    }
}
