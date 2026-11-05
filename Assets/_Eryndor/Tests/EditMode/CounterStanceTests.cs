// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using Eryndor.Core.Combat;
using NUnit.Framework;

namespace Eryndor.Tests
{
    public class CounterStanceTests
    {
        private const float Window = 0.3f;
        private const float Penalty = 0.6f;

        [Test]
        public void HitInsideWindow_IsCountered()
        {
            var s = new CounterStance();
            Assert.IsTrue(s.TryBegin(10f, Window, Penalty));
            Assert.AreEqual(CounterResult.Success, s.ResolveIncoming(10.2f, true));
        }

        [Test]
        public void HitAfterWindow_IsNotCountered()
        {
            var s = new CounterStance();
            s.TryBegin(10f, Window, Penalty);
            Assert.AreEqual(CounterResult.None, s.ResolveIncoming(10.35f, true));
        }

        [Test]
        public void UncounterableAttack_IsNeverCountered()
        {
            var s = new CounterStance();
            s.TryBegin(10f, Window, Penalty);
            Assert.AreEqual(CounterResult.None, s.ResolveIncoming(10.1f, false));
        }

        [Test]
        public void Whiff_LeavesPlayerVulnerable_AndBlocksNewCounter()
        {
            var s = new CounterStance();
            s.TryBegin(10f, Window, Penalty);
            Assert.IsTrue(s.IsVulnerable(10.5f));
            Assert.IsFalse(s.TryBegin(10.5f, Window, Penalty));
            Assert.IsFalse(s.IsVulnerable(10.95f));
            Assert.IsTrue(s.TryBegin(10.95f, Window, Penalty));
        }

        [Test]
        public void SuccessfulCounter_CancelsPenalty()
        {
            var s = new CounterStance();
            s.TryBegin(10f, Window, Penalty);
            s.ResolveIncoming(10.1f, true);
            Assert.IsFalse(s.IsVulnerable(10.5f));
            Assert.IsTrue(s.CanBegin(10.5f));
        }

        [Test]
        public void OneStance_CountersOnlyOneAttack()
        {
            var s = new CounterStance();
            s.TryBegin(10f, Window, Penalty);
            Assert.AreEqual(CounterResult.Success, s.ResolveIncoming(10.1f, true));
            Assert.AreEqual(CounterResult.None, s.ResolveIncoming(10.15f, true));
        }

        [Test]
        public void CannotBegin_WhileActive()
        {
            var s = new CounterStance();
            s.TryBegin(10f, Window, Penalty);
            Assert.IsFalse(s.TryBegin(10.1f, Window, Penalty));
        }
    }
}
