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
using Eryndor.Runtime.Player;
using UnityEngine;
using UnityEngine.AI;

namespace Eryndor.Runtime.Boss
{
    /// <summary>
    /// İlk Kat Bekçisi Gorrath (GDD 5.2). Davranış, çekirdekteki behavior tree düğümleriyle kurulur:
    ///
    /// Kök (öncelikli seçici)
    ///  1. Ölü mü?                 → hiçbir şey yapma
    ///  2. Sersemlemiş mi?         → bekle
    ///  3. Faz kontrolü            → can eşiğin altındaysa Öfke Fazına geç (bir kez)
    ///  4. Saldırıyı sürdür        → hazırlık → isabet → toparlanma
    ///  5. Uyum + normal saldırı   → oyuncu alışkanlığına göre ağırlıkları ayarla,
    ///                               menzilde saldırı seç, yoksa yaklaş
    /// Seçici her karede baştan bakar; sersemleme gibi öncelikli bir durum saldırıyı keser.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent), typeof(Combatant))]
    public sealed class BossController : MonoBehaviour, IStunnable, IGuardable
    {
        private enum AttackPhase { Windup, Recovery }

        [Header("Veri")]
        [SerializeField] private EnemyDefinition definition;
        [SerializeField] private CombatTuningAsset tuning;

        [Header("Sahne")]
        [SerializeField] private Animator animator;
        [SerializeField] private EssencePickup essencePickupPrefab;
        [SerializeField] private ItemPickup itemPickupPrefab;

        [Header("Uyum dalı (should-have)")]
        [SerializeField] private bool enableAdaptation = true;
        [SerializeField, Range(0f, 1f)] private float counterRelianceThreshold = 0.6f;
        [SerializeField, Range(0f, 1f)] private float spamRelianceThreshold = 0.6f;
        [SerializeField, Min(1f)] private float uncounterableBoost = 3f;
        [SerializeField, Range(0f, 1f)] private float feintChance = 0.3f;
        [SerializeField, Range(0.1f, 1f)] private float guardDamageMultiplier = 0.5f;

        private readonly IRandomSource _random = new SystemRandomSource();
        private PlayerHabitTracker _habits;
        private AttackSelector _selector;
        private BarkPicker _barks;
        private BTNode _root;

        private bool _active;
        private bool _enraged;
        private bool _adaptationAnnounced;
        private float _stunnedUntil;
        private float _guardUntil;

        private AttackData _currentAttack;
        private AttackPhase _phase;
        private float _phaseTimer;
        private bool _isFeint;

        public EnemyDefinition Definition => definition;
        public Combatant Combatant { get; private set; }
        public NavMeshAgent Agent { get; private set; }
        public Combatant Player { get; private set; }
        public bool IsActive => _active;
        public bool IsEnraged => _enraged;
        /// <summary>Hata ayıklama: en son çalışan dal.</summary>
        public string DebugBranch { get; private set; } = "-";

        public bool ResistsStagger => true;

        public event Action Activated;
        public event Action PhaseChanged;
        public event Action<AttackData> Telegraphed;

        private float WindupScale => _enraged ? 0.8f : 1f;
        private float DistanceToPlayer => Player != null
            ? CombatQuery.FlatDistance(transform.position, Player.transform.position)
            : float.PositiveInfinity;

        private void Awake()
        {
            Agent = GetComponent<NavMeshAgent>();
            Combatant = GetComponent<Combatant>();

            if (definition == null || tuning == null)
            {
                Debug.LogError("[BossController] EnemyDefinition ya da CombatTuningAsset atanmamış.", this);
                enabled = false;
                return;
            }

            Combatant.Initialize(new CharacterState(definition.Stats, tuning.Config, 0, definition.Defense, definition.MaxHpOverride));
            Combatant.Died += OnDied;
            Combatant.Damaged += (_, __) => CheckLowHealthBark();

            _habits = new PlayerHabitTracker(10);
            _selector = new AttackSelector(_random, maxRepeats: 2);
            if (definition.Barks != null)
                _barks = new BarkPicker(definition.Barks.Lines, _random, definition.Barks.CooldownSeconds);

            _root = BuildTree();
        }

        private void Start()
        {
            if (!enabled) return;
            var playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject == null) return;
            Player = playerObject.GetComponent<Combatant>();
            var playerCombat = playerObject.GetComponent<PlayerCombat>();
            if (playerCombat != null) playerCombat.ActionPerformed += _habits.Record;
        }

        /// <summary>Arena tetikleyicisi oyuncu salona girince çağırır.</summary>
        public void Activate()
        {
            if (_active || !Combatant.IsAlive) return;
            _active = true;
            Bark(BarkTrigger.BattleStart, true);
            Activated?.Invoke();
        }

        private void Update()
        {
            if (!_active || _root == null) return;
            if (Time.time >= _guardUntil) Combatant.DamageTakenMultiplier = 1f;
            _root.Tick(Time.deltaTime);
        }

        // ---------- Ağaç ----------

        private BTNode BuildTree()
        {
            return new Selector("Kök",
                new Sequence("Ölü",
                    new Condition("Ölü mü?", () => !Combatant.IsAlive),
                    new ActionNode("Bekle", _ => NodeStatus.Success)),

                new Sequence("Sersemleme",
                    new Condition("Sersemlemiş mi?", () => Time.time < _stunnedUntil),
                    new ActionNode("Sersemlemiş bekle", _ => { Branch("Sersemleme"); StopMoving(); return NodeStatus.Running; })),

                new Sequence("Faz kontrolü",
                    new Condition("Öfke eşiği?", ShouldEnrage),
                    new ActionNode("Öfke Fazına geç", _ => { EnterRage(); return NodeStatus.Success; })),

                new Sequence("Saldırıyı sürdür",
                    new Condition("Saldırı var mı?", () => _currentAttack != null),
                    new ActionNode("Saldırı", TickAttack, CancelAttack)),

                new Sequence("Uyum ve saldırı",
                    new ActionNode("Uyum", _ => { Adapt(); return NodeStatus.Success; }),
                    new Selector("Normal saldırı",
                        new ActionNode("Saldırı seç", _ => TryStartAttack()),
                        new ActionNode("Yaklaş", _ => ChasePlayer())))
            );
        }

        private void Branch(string name) => DebugBranch = name;

        private bool ShouldEnrage() =>
            !_enraged && definition.RageThreshold > 0f &&
            Combatant.State.CurrentHp <= Combatant.State.MaxHp * definition.RageThreshold;

        private void EnterRage()
        {
            Branch("Faz");
            _enraged = true;
            Agent.speed = definition.ChaseSpeed * definition.RageSpeedMultiplier;
            Bark(BarkTrigger.PhaseChange, true);
            Trigger("Rage");
            PhaseChanged?.Invoke();
        }

        /// <summary>Oyuncunun alışkanlığına göre davranış ağırlıklarını ayarlar (GDD 5.2, uyum dalı).</summary>
        private void Adapt()
        {
            if (!enableAdaptation) { _selector.UncounterableWeightMultiplier = 1f; return; }

            bool counterSpam = _habits.IsRelyingOn(PlayerActionType.Counter, counterRelianceThreshold);
            _selector.UncounterableWeightMultiplier = counterSpam ? uncounterableBoost : 1f;
            if (counterSpam && !_adaptationAnnounced)
            {
                _adaptationAnnounced = true;
                Bark(BarkTrigger.PlayerRepeatsCounter, true);
            }

            if (_habits.IsRelyingOn(PlayerActionType.LightAttack, spamRelianceThreshold))
            {
                _guardUntil = Time.time + 2f;
                Combatant.DamageTakenMultiplier = guardDamageMultiplier;
            }
        }

        private NodeStatus TryStartAttack()
        {
            if (Player == null || !Player.IsAlive) return NodeStatus.Failure;
            var attack = _selector.Pick(definition.Attacks, DistanceToPlayer);
            if (attack == null) return NodeStatus.Failure;

            Branch("Saldırı seç");
            StopMoving();
            _currentAttack = attack;
            _phase = AttackPhase.Windup;
            _phaseTimer = 0f;
            bool adapting = _selector.UncounterableWeightMultiplier > 1f;
            _isFeint = attack.counterable && adapting && _random.NextFloat() < feintChance;
            Telegraphed?.Invoke(attack);
            Trigger(attack.id);
            return NodeStatus.Success;
        }

        private NodeStatus TickAttack(float dt)
        {
            Branch("Saldırı: " + _currentAttack.id);
            _phaseTimer += dt;

            if (_phase == AttackPhase.Windup)
            {
                float windup = _currentAttack.windupTime * WindupScale;
                if (_phaseTimer < windup * 0.5f) FacePlayer(dt);
                if (_phaseTimer < windup) return NodeStatus.Running;

                // Sahte saldırı: isabet yok, oyuncunun erken counter'ı boşa gider
                if (!_isFeint) ResolveImpact();
                _phase = AttackPhase.Recovery;
                _phaseTimer = 0f;
                return NodeStatus.Running;
            }

            float recovery = _isFeint ? 0.1f : _currentAttack.recoveryTime;
            if (_phaseTimer < recovery) return NodeStatus.Running;

            _currentAttack = null;
            return NodeStatus.Success;
        }

        private void ResolveImpact()
        {
            if (Player == null || !Player.IsAlive) return;
            Vector3 to = Player.transform.position - transform.position;
            to.y = 0f;
            bool inArc = _currentAttack.isArea || Vector3.Angle(transform.forward, to) <= 70f;
            if (DistanceToPlayer > _currentAttack.maxRange + 0.5f || !inArc) return;

            float damage = DamageCalculator.Compute(tuning.Config, _currentAttack.baseDamage, definition.Stats.strength,
                                                    _enraged ? 1.15f : 1f);
            Player.ReceiveHit(new HitInfo(Combatant, _currentAttack, damage, _currentAttack.counterable));
        }

        private void CancelAttack()
        {
            _currentAttack = null;
            _isFeint = false;
        }

        private NodeStatus ChasePlayer()
        {
            if (Player == null || !Player.IsAlive || !Agent.isOnNavMesh) return NodeStatus.Failure;
            Branch("Yaklaş");
            Agent.isStopped = false;
            Agent.speed = definition.ChaseSpeed * (_enraged ? definition.RageSpeedMultiplier : 1f);
            Agent.SetDestination(Player.transform.position);
            return NodeStatus.Running;
        }

        private void StopMoving()
        {
            if (Agent.isOnNavMesh) { Agent.isStopped = true; Agent.ResetPath(); }
        }

        private void FacePlayer(float dt)
        {
            if (Player == null) return;
            Vector3 dir = Player.transform.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), 360f * dt);
        }

        // ---------- Arayüzler ----------

        public void Stun(float seconds)
        {
            _stunnedUntil = Mathf.Max(_stunnedUntil, Time.time + seconds);
            Trigger("Stunned");
        }

        /// <summary>Demir Avuç savunma duruşunu kırar.</summary>
        public void BreakGuard()
        {
            _guardUntil = 0f;
            Combatant.DamageTakenMultiplier = 1f;
        }

        private void CheckLowHealthBark()
        {
            var s = Combatant.State;
            if (s.CurrentHp > 0f && s.CurrentHp <= s.MaxHp * 0.2f) Bark(BarkTrigger.LowHealth, false);
        }

        private void Bark(BarkTrigger trigger, bool force)
        {
            if (_barks == null) return;
            BarkBus.Say(definition.DisplayName, _barks.TryPick(trigger, Time.time, force));
        }

        private void OnDied(Combatant _)
        {
            Bark(BarkTrigger.Death, true);
            CancelAttack();
            if (Agent.isOnNavMesh) Agent.isStopped = true;
            Trigger("Die");
            DropSpawner.SpawnDrops(definition, transform.position, essencePickupPrefab, itemPickupPrefab, _random);
            GameManager.Instance?.OnBossDefeated();
        }

        private void Trigger(string name)
        {
            if (animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null)
                animator.SetTrigger(name);
        }
    }
}
