// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using System.Collections.Generic;
using Eryndor.Core.AI;
using Eryndor.Core.Barks;
using Eryndor.Core.Combat;
using NUnit.Framework;

namespace Eryndor.Tests
{
    public class BehaviorTreeTests
    {
        [Test]
        public void Selector_ReturnsFirstNonFailingChild()
        {
            int ran = 0;
            var tree = new Selector("root",
                new Condition("no", () => false),
                new ActionNode("yes", _ => { ran++; return NodeStatus.Success; }),
                new ActionNode("never", _ => { ran += 100; return NodeStatus.Success; }));
            Assert.AreEqual(NodeStatus.Success, tree.Tick(0f));
            Assert.AreEqual(1, ran);
        }

        [Test]
        public void Sequence_ResumesFromRunningChild()
        {
            int firstRuns = 0, ticks = 0;
            var seq = new Sequence("seq",
                new ActionNode("first", _ => { firstRuns++; return NodeStatus.Success; }),
                new ActionNode("long", _ => ++ticks < 3 ? NodeStatus.Running : NodeStatus.Success));

            Assert.AreEqual(NodeStatus.Running, seq.Tick(0f));
            Assert.AreEqual(NodeStatus.Running, seq.Tick(0f));
            Assert.AreEqual(NodeStatus.Success, seq.Tick(0f));
            Assert.AreEqual(1, firstRuns); // ilk çocuk tekrar çalışmadı
        }

        [Test]
        public void Selector_HigherPriorityBranch_InterruptsAndResetsRunningBranch()
        {
            bool stunned = false;
            bool attackCancelled = false;
            var tree = new Selector("root",
                new Sequence("stun", new Condition("stunned?", () => stunned), new ActionNode("wait", _ => NodeStatus.Running)),
                new ActionNode("attack", _ => NodeStatus.Running, () => attackCancelled = true));

            tree.Tick(0f);                 // saldırı çalışıyor
            Assert.IsFalse(attackCancelled);
            stunned = true;
            tree.Tick(0f);                 // sersemleme öncelikli
            Assert.IsTrue(attackCancelled);
        }

        [Test]
        public void Inverter_FlipsResult()
        {
            Assert.AreEqual(NodeStatus.Failure, new Inverter("inv", new Condition("t", () => true)).Tick(0f));
            Assert.AreEqual(NodeStatus.Running, new Inverter("inv", new ActionNode("r", _ => NodeStatus.Running)).Tick(0f));
        }
    }

    public class StateMachineTests
    {
        private sealed class Ctx { public List<string> Log = new List<string>(); }

        private sealed class LogState : IState<Ctx>
        {
            public string Name { get; }
            public LogState(string name) { Name = name; }
            public void Enter(Ctx c) => c.Log.Add("enter " + Name);
            public void Tick(Ctx c, float dt) => c.Log.Add("tick " + Name);
            public void Exit(Ctx c) => c.Log.Add("exit " + Name);
        }

        [Test]
        public void ChangeState_CallsExitThenEnter_AndResetsTimer()
        {
            var ctx = new Ctx();
            var fsm = new StateMachine<Ctx>(ctx);
            var a = new LogState("A");
            var b = new LogState("B");

            fsm.ChangeState(a);
            fsm.Tick(1.5f);
            Assert.AreEqual(1.5f, fsm.TimeInState, 0.001f);

            fsm.ChangeState(b);
            Assert.AreEqual(0f, fsm.TimeInState);
            CollectionAssert.AreEqual(new[] { "enter A", "tick A", "exit A", "enter B" }, ctx.Log);
        }
    }

    public class BossLogicTests
    {
        private static AttackData A(string id, float min, float max, bool counterable = true, float weight = 1f) =>
            new AttackData { id = id, minRange = min, maxRange = max, counterable = counterable, weight = weight };

        [Test]
        public void AttackSelector_OnlyPicksAttacksInRange()
        {
            var attacks = new List<AttackData> { A("near", 0f, 2f), A("far", 3f, 8f) };
            var selector = new AttackSelector(new FakeRandom(0f, 0f));
            Assert.AreEqual("far", selector.Pick(attacks, 5f).id);
            Assert.IsNull(selector.Pick(attacks, 20f));
        }

        [Test]
        public void AttackSelector_NeverRepeatsMoreThanTwice()
        {
            var attacks = new List<AttackData> { A("slam", 0f, 3f), A("combo", 0f, 3f) };
            var selector = new AttackSelector(new FakeRandom(0f, 0f, 0f, 0f), maxRepeats: 2);
            Assert.AreEqual("slam", selector.Pick(attacks, 1f).id);
            Assert.AreEqual("slam", selector.Pick(attacks, 1f).id);
            Assert.AreEqual("combo", selector.Pick(attacks, 1f).id); // üçüncü slam engellendi
        }

        [Test]
        public void AttackSelector_AdaptationBoostsUncounterable()
        {
            var attacks = new List<AttackData> { A("slam", 0f, 3f), A("wave", 0f, 3f, counterable: false) };
            // Ağırlıklar 1 ve 1×3 → toplam 4. 0.3 × 4 = 1.2 > 1 → "wave"
            var selector = new AttackSelector(new FakeRandom(0.3f)) { UncounterableWeightMultiplier = 3f };
            Assert.AreEqual("wave", selector.Pick(attacks, 1f).id);
        }

        [Test]
        public void HabitTracker_DetectsCounterReliance()
        {
            var t = new PlayerHabitTracker(10);
            for (int i = 0; i < 7; i++) t.Record(PlayerActionType.Counter);
            for (int i = 0; i < 3; i++) t.Record(PlayerActionType.LightAttack);
            Assert.AreEqual(0.7f, t.Share(PlayerActionType.Counter), 0.001f);
            Assert.IsTrue(t.IsRelyingOn(PlayerActionType.Counter, 0.6f));
            Assert.IsFalse(t.IsRelyingOn(PlayerActionType.LightAttack, 0.6f));
        }

        [Test]
        public void HabitTracker_ForgetsOldMoves()
        {
            var t = new PlayerHabitTracker(4);
            for (int i = 0; i < 4; i++) t.Record(PlayerActionType.Counter);
            for (int i = 0; i < 4; i++) t.Record(PlayerActionType.Dodge);
            Assert.AreEqual(0f, t.Share(PlayerActionType.Counter));
        }

        [Test]
        public void HabitTracker_NeedsMinimumSamples()
        {
            var t = new PlayerHabitTracker(10);
            t.Record(PlayerActionType.Counter);
            Assert.IsFalse(t.IsRelyingOn(PlayerActionType.Counter, 0.6f)); // 1 hamle yetmez
        }
    }

    public class BarkPickerTests
    {
        private static readonly BarkLine[] Lines =
        {
            new BarkLine(BarkTrigger.Countered, "İmkansız!"),
            new BarkLine(BarkTrigger.Countered, "Nasıl?"),
            new BarkLine(BarkTrigger.BattleStart, "Silahsız mı geldin?")
        };

        [Test]
        public void ReturnsLineForTrigger_RespectsCooldown()
        {
            var p = new BarkPicker(Lines, new FakeRandom(), 4f);
            Assert.AreEqual("Silahsız mı geldin?", p.TryPick(BarkTrigger.BattleStart, 0f));
            Assert.IsNull(p.TryPick(BarkTrigger.Countered, 1f));         // bekleme süresi
            Assert.IsNotNull(p.TryPick(BarkTrigger.Countered, 1f, true)); // zorla
        }

        [Test]
        public void DoesNotRepeatSameLineBackToBack()
        {
            var p = new BarkPicker(Lines, new FakeRandom(), 0f);
            string first = p.TryPick(BarkTrigger.Countered, 0f);
            string second = p.TryPick(BarkTrigger.Countered, 1f);
            Assert.AreNotEqual(first, second);
        }

        [Test]
        public void UnknownTrigger_ReturnsNull()
        {
            var p = new BarkPicker(Lines, new FakeRandom(), 0f);
            Assert.IsNull(p.TryPick(BarkTrigger.Death, 0f));
        }
    }
}
