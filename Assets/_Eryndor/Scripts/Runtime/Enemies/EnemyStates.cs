// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using Eryndor.Core.AI;
using Eryndor.Core.Barks;
using Eryndor.Runtime.Combat;
using UnityEngine;
using UnityEngine.AI;

namespace Eryndor.Runtime.Enemies
{
    /// <summary>Ortak yardımcılar.</summary>
    public abstract class EnemyStateBase : IState<EnemyController>
    {
        public abstract string Name { get; }
        public virtual void Enter(EnemyController e) { }
        public abstract void Tick(EnemyController e, float dt);
        public virtual void Exit(EnemyController e) { }

        protected static void Stop(EnemyController e)
        {
            if (e.Agent.isOnNavMesh) { e.Agent.isStopped = true; e.Agent.ResetPath(); }
        }

        protected static void MoveTo(EnemyController e, Vector3 target, float speed)
        {
            if (!e.Agent.isOnNavMesh) return;
            e.Agent.isStopped = false;
            e.Agent.speed = speed;
            e.Agent.SetDestination(target);
        }

        /// <summary>Yol bulunamadıysa true (GDD 5.4 fallback).</summary>
        protected static bool PathFailed(EnemyController e) =>
            e.Agent.isOnNavMesh && !e.Agent.pathPending && e.Agent.pathStatus == NavMeshPathStatus.PathInvalid;
    }

    /// <summary>Devriye: noktalar arasında yürür.</summary>
    public sealed class PatrolState : EnemyStateBase
    {
        private int _index;
        public override string Name => "Patrol";

        public override void Enter(EnemyController e)
        {
            e.CurrentAttack = null;
            GoToCurrentPoint(e);
        }

        public override void Tick(EnemyController e, float dt)
        {
            if (e.CanSeePlayer() || e.CanHearPlayer()) { e.Machine.ChangeState(e.Suspicious); return; }

            var points = e.PatrolPoints;
            if (points.Length == 0 || !e.Agent.isOnNavMesh) return;
            if (!e.Agent.pathPending && e.Agent.remainingDistance <= e.Agent.stoppingDistance + 0.2f)
            {
                _index = (_index + 1) % points.Length;
                GoToCurrentPoint(e);
            }
        }

        private void GoToCurrentPoint(EnemyController e)
        {
            var points = e.PatrolPoints;
            if (points.Length == 0) { Stop(e); return; }
            if (points[_index] != null) MoveTo(e, points[_index].position, e.Definition.PatrolSpeed);
        }
    }

    /// <summary>Şüphe: durur, "?" gösterir. Oyuncuyu yeterince görürse kovalar.</summary>
    public sealed class SuspiciousState : EnemyStateBase
    {
        private float _noticeTimer;
        public override string Name => "Suspicious";

        public override void Enter(EnemyController e)
        {
            _noticeTimer = 0f;
            Stop(e);
        }

        public override void Tick(EnemyController e, float dt)
        {
            if (e.CanSeePlayer())
            {
                e.FacePlayer();
                _noticeTimer += dt;
                if (_noticeTimer >= e.Definition.SuspicionTime)
                {
                    e.Bark(BarkTrigger.PlayerSpotted);
                    e.Machine.ChangeState(e.Chase);
                }
                return;
            }

            if (e.CanHearPlayer())
            {
                Vector3 dir = e.LastKnownPlayerPosition - e.transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.01f)
                    e.transform.rotation = Quaternion.RotateTowards(e.transform.rotation, Quaternion.LookRotation(dir), 360f * dt);
            }

            if (e.Machine.TimeInState > 3f) e.Machine.ChangeState(e.Patrol);
        }
    }

    /// <summary>Kovalama: NavMesh ile oyuncuya koşar, menzile girince saldırıya hazırlanır.</summary>
    public sealed class ChaseState : EnemyStateBase
    {
        private float _repathTimer;
        private float _noProgressTimer;
        private float _lastDistance;
        public override string Name => "Chase";

        public override void Enter(EnemyController e)
        {
            _repathTimer = 0f;
            _noProgressTimer = 0f;
            _lastDistance = e.DistanceToPlayer;
        }

        public override void Tick(EnemyController e, float dt)
        {
            if (!e.PlayerAlive) { e.Machine.ChangeState(e.Patrol); return; }

            bool sees = e.CanSeePlayer();
            float distance = e.DistanceToPlayer;

            var attack = sees ? e.PickAttack(distance) : null;
            if (attack != null)
            {
                e.CurrentAttack = attack;
                e.Machine.ChangeState(e.Windup);
                return;
            }

            // Oyuncuyu 4 sn görmediyse ya da yol yoksa vazgeç
            if (Time.time - e.LastSeenTime > 4f || PathFailed(e)) { e.Machine.ChangeState(e.Patrol); return; }

            // Takılma kontrolü: mesafe kapanmıyorsa
            if (distance < _lastDistance - 0.05f) { _noProgressTimer = 0f; _lastDistance = distance; }
            else _noProgressTimer += dt;
            if (_noProgressTimer > e.StuckTimeout) { e.Machine.ChangeState(e.Patrol); return; }

            _repathTimer -= dt;
            if (_repathTimer <= 0f)
            {
                _repathTimer = 0.2f;
                MoveTo(e, sees ? e.Player.transform.position : e.LastKnownPlayerPosition, e.Definition.ChaseSpeed);
            }
        }
    }

    /// <summary>
    /// Saldırı hazırlığı (telegraph). Counter penceresi bu durumun sonunda, isabet anında kullanılır.
    /// Sarı uyarı = counter'lanabilir, kırmızı = kaç.
    /// </summary>
    public sealed class WindupState : EnemyStateBase
    {
        public override string Name => "Windup";

        public override void Enter(EnemyController e)
        {
            Stop(e);
            e.RaiseTelegraph(e.CurrentAttack);
        }

        public override void Tick(EnemyController e, float dt)
        {
            if (e.CurrentAttack == null) { e.Machine.ChangeState(e.Chase); return; }
            // Hazırlığın ilk yarısında oyuncuya döner, sonra yönü kilitler (oyuncu kaçabilsin diye)
            if (e.Machine.TimeInState < e.CurrentAttack.windupTime * 0.5f) e.FacePlayer();
            if (e.Machine.TimeInState >= e.CurrentAttack.windupTime) e.Machine.ChangeState(e.Attack);
        }
    }

    /// <summary>Saldırı: isabet anı Enter'da, sonra toparlanma süresi.</summary>
    public sealed class AttackState : EnemyStateBase
    {
        public override string Name => "Attack";

        public override void Enter(EnemyController e)
        {
            var attack = e.CurrentAttack;
            if (attack == null || !e.PlayerAlive) return;

            float distance = e.DistanceToPlayer;
            Vector3 to = e.Player.transform.position - e.transform.position;
            to.y = 0f;
            bool inArc = attack.isArea || Vector3.Angle(e.transform.forward, to) <= 70f;
            if (distance > attack.maxRange + 0.5f || !inArc) return; // ıskaladı

            float damage = Eryndor.Core.Combat.DamageCalculator.Compute(e.Config, attack.baseDamage, e.Definition.Stats.strength);
            var outcome = e.Player.ReceiveHit(new HitInfo(e.Combatant, attack, damage, attack.counterable));
            if (outcome == HitOutcome.Countered) e.Bark(BarkTrigger.Countered);
        }

        public override void Tick(EnemyController e, float dt)
        {
            float recovery = e.CurrentAttack != null ? e.CurrentAttack.recoveryTime : 0.5f;
            if (e.Machine.TimeInState >= recovery) e.Machine.ChangeState(e.Chase);
        }

        public override void Exit(EnemyController e) => e.CurrentAttack = null;
    }

    /// <summary>Sersemleme: hareketsiz bekler, sonra kovalamaya döner.</summary>
    public sealed class StunnedState : EnemyStateBase
    {
        public override string Name => "Stunned";

        public override void Enter(EnemyController e)
        {
            Stop(e);
            e.CurrentAttack = null;
        }

        public override void Tick(EnemyController e, float dt)
        {
            if (e.Machine.TimeInState >= e.StunDuration) e.Machine.ChangeState(e.Chase);
        }
    }
}
