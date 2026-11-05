// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using System;
using Eryndor.Core.AI;
using Eryndor.Core.Barks;
using Eryndor.Core.Characters;
using Eryndor.Core.Combat;
using Eryndor.Core.Util;
using Eryndor.Data;
using Eryndor.Runtime.Combat;
using Eryndor.Runtime.Game;
using Eryndor.Runtime.Pickups;
using UnityEngine;
using UnityEngine.AI;

namespace Eryndor.Runtime.Enemies
{
    /// <summary>
    /// Sıradan canavarın beyni (GDD 5.1). Davranış EnemyStates.cs içindeki durumlarla,
    /// çekirdekteki StateMachine üzerinden çalışır. Algı (görme/duyma) ve ölüm/ödül burada.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent), typeof(Combatant))]
    public sealed class EnemyController : MonoBehaviour, IStunnable
    {
        [Header("Veri")]
        [SerializeField] private EnemyDefinition definition;
        [SerializeField] private CombatTuningAsset tuning;

        [Header("Sahne")]
        [SerializeField] private Transform[] patrolPoints = Array.Empty<Transform>();
        [Tooltip("Görüşü engelleyen katmanlar (duvarlar).")]
        [SerializeField] private LayerMask sightBlockers;
        [SerializeField] private Transform eyes;
        [SerializeField] private Animator animator;

        [Header("Ödüller")]
        [SerializeField] private EssencePickup essencePickupPrefab;
        [SerializeField] private ItemPickup itemPickupPrefab;

        [Header("Fallback")]
        [Tooltip("Bir durumda bu kadar saniye ilerleme olmazsa Devriye'ye sıfırlanır (GDD 5.4).")]
        [SerializeField] private float stuckTimeout = 5f;

        private static readonly IRandomSource SharedRandom = new SystemRandomSource();

        private BarkPicker _barks;
        private float _pendingStun;
        private float _blindUntil;
        private int _attackCursor;

        public EnemyDefinition Definition => definition;
        public CombatConfig Config => tuning.Config;
        public NavMeshAgent Agent { get; private set; }
        public Combatant Combatant { get; private set; }
        public Combatant Player { get; private set; }
        public StateMachine<EnemyController> Machine { get; private set; }
        public Transform[] PatrolPoints => patrolPoints;
        public float StuckTimeout => stuckTimeout;

        public Vector3 LastKnownPlayerPosition { get; set; }
        public float LastSeenTime { get; private set; } = float.NegativeInfinity;
        public AttackData CurrentAttack { get; set; }
        public float StunDuration { get; private set; }

        // Durum örnekleri (tekrar tekrar new yapmamak için)
        public readonly PatrolState Patrol = new PatrolState();
        public readonly SuspiciousState Suspicious = new SuspiciousState();
        public readonly ChaseState Chase = new ChaseState();
        public readonly WindupState Windup = new WindupState();
        public readonly AttackState Attack = new AttackState();
        public readonly StunnedState Stunned = new StunnedState();

        /// <summary>Durum adı değişince (göstergeler: "?" ve "!").</summary>
        public event Action<string> StateNameChanged;
        /// <summary>Saldırı uyarısı başladı (counter'lanabilir mi bilgisiyle).</summary>
        public event Action<AttackData> Telegraphed;

        public bool ResistsStagger => false;

        private void Awake()
        {
            Agent = GetComponent<NavMeshAgent>();
            Combatant = GetComponent<Combatant>();

            if (definition == null || tuning == null)
            {
                Debug.LogError("[EnemyController] EnemyDefinition ya da CombatTuningAsset atanmamış.", this);
                enabled = false;
                return;
            }

            var state = new CharacterState(definition.Stats, tuning.Config, 0, definition.Defense, definition.MaxHpOverride);
            Combatant.Initialize(state);
            Combatant.Died += OnDied;
            Combatant.Damaged += (hit, _) => { if (hit.source != null) LastKnownPlayerPosition = hit.source.transform.position; };

            if (definition.Barks != null)
                _barks = new BarkPicker(definition.Barks.Lines, SharedRandom, definition.Barks.CooldownSeconds);

            Machine = new StateMachine<EnemyController>(this);
            Machine.StateChanged += (_, next) =>
            {
                StateNameChanged?.Invoke(next.Name);
                if (animator != null && animator.runtimeAnimatorController != null) animator.SetTrigger(next.Name);
            };
        }

        private void OnEnable() => SmokeBomb.Detonated += OnSmoke;
        private void OnDisable() => SmokeBomb.Detonated -= OnSmoke;

        private void Start()
        {
            if (!enabled) return;
            var playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null) Player = playerObject.GetComponent<Combatant>();
            Machine.ChangeState(Patrol);
        }

        private void Update()
        {
            if (!Combatant.IsAlive) return;

            if (_pendingStun > 0f)
            {
                StunDuration = _pendingStun;
                _pendingStun = 0f;
                Machine.ChangeState(Stunned);
            }

            Machine.Tick(Time.deltaTime);
            if (animator != null) animator.SetFloat("Speed", Agent.velocity.magnitude);
        }

        // ---------- Algı ----------

        public bool PlayerAlive => Player != null && Player.IsAlive;

        public float DistanceToPlayer => PlayerAlive
            ? CombatQuery.FlatDistance(transform.position, Player.transform.position)
            : float.PositiveInfinity;

        public bool CanSeePlayer()
        {
            if (!PlayerAlive || Time.time < _blindUntil) return false;
            Vector3 eye = eyes ? eyes.position : transform.position + Vector3.up * 1.6f;
            Vector3 target = Player.AimPosition;
            Vector3 to = target - eye;
            if (to.magnitude > definition.SightRange) return false;

            Vector3 flat = new Vector3(to.x, 0f, to.z);
            if (Vector3.Angle(transform.forward, flat) > definition.SightAngle * 0.5f) return false;
            if (Physics.Raycast(eye, to.normalized, to.magnitude, sightBlockers, QueryTriggerInteraction.Ignore)) return false;

            LastSeenTime = Time.time;
            LastKnownPlayerPosition = Player.transform.position;
            return true;
        }

        public bool CanHearPlayer() =>
            PlayerAlive && Time.time >= _blindUntil && DistanceToPlayer <= definition.HearingRange;

        // ---------- Saldırı yardımcıları ----------

        /// <summary>Menzile uyan sıradaki saldırıyı seçer (sırayla döner). Yoksa null.</summary>
        public AttackData PickAttack(float distance)
        {
            var attacks = definition.Attacks;
            for (int i = 0; i < attacks.Count; i++)
            {
                var a = attacks[(_attackCursor + i) % attacks.Count];
                if (a != null && distance >= a.minRange && distance <= a.maxRange)
                {
                    _attackCursor = (_attackCursor + i + 1) % attacks.Count;
                    return a;
                }
            }
            return null;
        }

        public void RaiseTelegraph(AttackData attack) => Telegraphed?.Invoke(attack);

        public void FacePlayer()
        {
            if (!PlayerAlive) return;
            Vector3 dir = Player.transform.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), 540f * Time.deltaTime);
        }

        public void Bark(BarkTrigger trigger)
        {
            if (_barks == null) return;
            BarkBus.Say(definition.DisplayName, _barks.TryPick(trigger, Time.time));
        }

        // ---------- IStunnable / duman / ölüm ----------

        /// <summary>Sersemleme bir sonraki karede uygulanır; böylece durum geçişi Enter() içinden olmaz.</summary>
        public void Stun(float seconds)
        {
            if (Combatant.IsAlive) _pendingStun = Mathf.Max(_pendingStun, seconds);
        }

        private void OnSmoke(float seconds)
        {
            _blindUntil = Time.time + seconds;
            if (Combatant.IsAlive && Machine?.Current != Stunned) Machine.ChangeState(Patrol);
        }

        private void OnDied(Combatant _)
        {
            Bark(BarkTrigger.Death);
            if (Agent.isOnNavMesh) Agent.isStopped = true;
            Agent.enabled = false;
            if (animator != null && animator.runtimeAnimatorController != null) animator.SetTrigger("Die");

            GameManager.Instance?.Stats.AddKill();
            DropSpawner.SpawnDrops(definition, transform.position, essencePickupPrefab, itemPickupPrefab, SharedRandom);
            Destroy(gameObject, 3f);
        }
    }
}
