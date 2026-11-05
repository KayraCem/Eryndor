// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using System;
using System.Collections;
using System.Collections.Generic;
using Eryndor.Core.AI;
using Eryndor.Core.Characters;
using Eryndor.Core.Combat;
using Eryndor.Core.Essence;
using Eryndor.Core.Stats;
using Eryndor.Data;
using Eryndor.Runtime.Combat;
using Eryndor.Runtime.Pickups;
using UnityEngine;

namespace Eryndor.Runtime.Player
{
    /// <summary>
    /// Dövüş Sanatçısı'nın dövüş sistemi: silahsız kombo, ağır vuruş, counter, kaçış,
    /// Mana teknikleri, Essence emme ve Essence Güçlendirme (GDD Bölüm 4).
    /// Sayısal değerler CombatTuningAsset ve CharacterDefinition'dan gelir.
    /// </summary>
    [RequireComponent(typeof(Combatant), typeof(PlayerMotor), typeof(PlayerInputReader))]
    public sealed class PlayerCombat : MonoBehaviour
    {
        [Header("Veri")]
        [SerializeField] private CharacterDefinition definition;
        [SerializeField] private CombatTuningAsset tuning;

        [Header("Sahne")]
        [Tooltip("İsteğe bağlı. Trigger adları: light, heavy, counter, CounterStrike, Dodge, Technique, Hit, Die")]
        [SerializeField] private Animator animator;
        [SerializeField] private LayerMask hitMask = ~0;

        [Header("Ayarlar")]
        [SerializeField] private float comboResetTime = 0.8f;
        [SerializeField, Range(10f, 180f)] private float attackHalfAngle = 60f;
        [SerializeField] private float interactRadius = 2.5f;
        [SerializeField] private float heavyStaggerSeconds = 0.6f;

        private Combatant _combatant;
        private PlayerMotor _motor;
        private PlayerInputReader _input;
        private CharacterState _state;
        private readonly CounterStance _stance = new CounterStance();
        private readonly List<Combatant> _targets = new List<Combatant>();
        private readonly Collider[] _interactBuffer = new Collider[16];

        private int _comboIndex;
        private float _lastLightTime = -10f;
        private float _busyUntil;
        private float _empowerEndsAt = -1f;
        private float _empowerReadyAt;
        private float _waterStanceUntil;
        private float _waterMultiplier = 1f;
        private float[] _techniqueReadyAt = Array.Empty<float>();
        private int _nextReplaceIndex;

        public CharacterDefinition Definition => definition;
        public CombatConfig Config => tuning != null ? tuning.Config : null;
        public CharacterState State => _state;
        public Combatant Combatant => _combatant;
        public bool IsEmpowered => _state != null && _state.Essences.IsEmpowered;
        public bool IsCounterStanceActive => _stance.IsActive(Time.time);
        public bool IsBusy => Time.time < _busyUntil || _stance.IsVulnerable(Time.time) || (_motor != null && _motor.IsDashing);

        /// <summary>Essence Güçlendirme bekleme süresinin kalan oranı (0 = hazır).</summary>
        public float EmpowerCooldown01
        {
            get
            {
                if (Config == null) return 0f;
                float total = Config.empowerDuration + Config.empowerCooldown;
                return Mathf.Clamp01((_empowerReadyAt - Time.time) / Mathf.Max(0.01f, total));
            }
        }

        /// <summary>Boss'un uyum dalı ve istatistikler bu olayı dinler.</summary>
        public event Action<PlayerActionType> ActionPerformed;
        public event Action<Combatant> CounterSucceeded;
        /// <summary>(emilen, yerinden çıkan ya da null)</summary>
        public event Action<EssenceData, EssenceData> EssenceAbsorbed;
        /// <summary>Slotlar doluyken Essence emilmek istendi; EssenceSwapUI oyuncuya slot seçtirir.</summary>
        public event Action<EssencePickup> EssenceSwapRequested;

        private void Awake()
        {
            _combatant = GetComponent<Combatant>();
            _motor = GetComponent<PlayerMotor>();
            _input = GetComponent<PlayerInputReader>();

            if (definition == null || tuning == null)
            {
                Debug.LogError("[PlayerCombat] CharacterDefinition ya da CombatTuningAsset atanmamış.", this);
                enabled = false;
                return;
            }

            _state = new CharacterState(definition.BaseStats, tuning.Config, definition.EssenceSlots);
            _combatant.Initialize(_state);
            _combatant.CounterInterceptor = TryCounter;
            _combatant.Damaged += (_, __) => Trigger("Hit");
            _combatant.Died += _ => Trigger("Die");

            _motor.BaseSpeed = definition.MoveSpeed;
            _state.Essences.Changed += RefreshMoveSpeed;
            _techniqueReadyAt = new float[definition.Techniques.Count];
        }

        private void Update()
        {
            if (_state == null) return;
            if (!_combatant.IsAlive) { _motor.Tick(Vector2.zero); return; }

            _state.RegenerateStamina(Time.deltaTime);
            UpdateEmpower();
            HandleInput();
            _motor.Tick(_input.Move);

            if (animator != null) animator.SetFloat("Speed", _motor.CurrentSpeed01);
        }

        // ---------- Girdi ----------

        private void HandleInput()
        {
            if (_input.EmpowerHeld) TryEmpower();
            if (IsBusy) return;

            if (_input.CounterPressed) TryBeginCounter();
            else if (_input.DodgePressed) TryDodge();
            else if (_input.LightPressed) DoLightAttack();
            else if (_input.HeavyPressed) DoHeavyAttack();
            else if (_input.Technique1Pressed) TryTechnique(0);
            else if (_input.Technique2Pressed) TryTechnique(1);
            else if (_input.InteractPressed) TryAbsorbNearbyEssence();
        }

        // ---------- Counter ----------

        public float CurrentCounterWindow()
        {
            float window = StatFormulas.CounterWindow(Config, _state.EffectiveStats.agility);
            if (Time.time < _waterStanceUntil) window *= _waterMultiplier;
            return window;
        }

        private void TryBeginCounter()
        {
            if (!_stance.TryBegin(Time.time, CurrentCounterWindow(), Config.counterWhiffPenalty)) return;
            Trigger("counter");
            ActionPerformed?.Invoke(PlayerActionType.Counter);
        }

        /// <summary>Combatant gelen her vuruşta bunu sorar. true = counter başarılı, hasar yok.</summary>
        private bool TryCounter(HitInfo hit)
        {
            if (_stance.ResolveIncoming(Time.time, hit.counterable) != CounterResult.Success) return false;

            var source = hit.source;
            if (source != null && source.IsAlive)
            {
                _motor.FaceTowards(source.transform.position);
                float bonus = _state.Essences.Total(EssenceBonusType.CounterDamagePercent);
                float damage = DamageCalculator.CounterDamage(Config, definition.CounterStrike.baseDamage,
                                                              _state.EffectiveStats.strength, bonus);
                source.ReceiveHit(new HitInfo(_combatant, definition.CounterStrike, damage, false));
                var stunnable = source.GetComponent<IStunnable>();
                if (stunnable != null) stunnable.Stun(Config.counterStunDuration);
            }

            _state.GainMana(Config.counterManaGain);
            float invulnerable = _state.Essences.Total(EssenceBonusType.PostCounterInvulnerabilitySeconds);
            if (invulnerable > 0f) _state.SetInvulnerableUntil(Time.time + invulnerable);

            Trigger("CounterStrike");
            CounterSucceeded?.Invoke(source);
            return true;
        }

        // ---------- Kaçış ----------

        private void TryDodge()
        {
            if (!_state.TrySpendStamina(Config.dodgeStaminaCost)) return;
            Vector3 dir = _motor.CameraRelative(_input.Move);
            _motor.Dash(dir.sqrMagnitude > 0.01f ? dir : transform.forward, definition.DodgeDistance, definition.DodgeDuration);
            _state.SetInvulnerableUntil(Time.time + Config.dodgeInvulnerability);
            Trigger("Dodge");
            ActionPerformed?.Invoke(PlayerActionType.Dodge);
        }

        // ---------- Vuruşlar ----------

        private void DoLightAttack()
        {
            if (Time.time - _lastLightTime > comboResetTime) _comboIndex = 0;
            float multiplier = _comboIndex == 2 ? Config.comboFinisherMultiplier : 1f;
            StartCoroutine(PerformAttack(definition.LightAttack, multiplier, 0f));
            _comboIndex = (_comboIndex + 1) % 3;
            _lastLightTime = Time.time;
            ActionPerformed?.Invoke(PlayerActionType.LightAttack);
        }

        private void DoHeavyAttack()
        {
            StartCoroutine(PerformAttack(definition.HeavyAttack, 1f, heavyStaggerSeconds));
            ActionPerformed?.Invoke(PlayerActionType.HeavyAttack);
        }

        private IEnumerator PerformAttack(AttackData attack, float multiplier, float staggerSeconds)
        {
            float total = attack.windupTime + attack.recoveryTime;
            _busyUntil = Time.time + total;
            _motor.LockMovement(total);
            Trigger(attack.id);

            yield return new WaitForSeconds(attack.windupTime);
            if (!_combatant.IsAlive) yield break;

            HitArea(attack, attack.baseDamage, multiplier, attack.maxRange, attackHalfAngle, staggerSeconds, false);
        }

        /// <summary>Önündeki (ya da halfAngle 180 ise etrafındaki) rakiplere hasar verir. İsabet sayısını döner.</summary>
        private int HitArea(AttackData attack, float baseDamage, float multiplier, float range, float halfAngle,
                            float staggerSeconds, bool breaksGuard)
        {
            CombatQuery.FindTargets(transform.position + Vector3.up, transform.forward, range, halfAngle,
                                    Team.Player, _targets, hitMask);
            int hits = 0;
            foreach (var target in _targets)
            {
                if (breaksGuard) target.GetComponent<IGuardable>()?.BreakGuard();

                float damage = DamageCalculator.Compute(Config, baseDamage, _state.EffectiveStats.strength, multiplier);
                var outcome = target.ReceiveHit(new HitInfo(_combatant, attack, damage, false));
                if (outcome != HitOutcome.Hit && outcome != HitOutcome.Killed) continue;

                hits++;
                _state.GainMana(Config.manaPerHit);
                if (staggerSeconds > 0f && outcome == HitOutcome.Hit)
                {
                    var stunnable = target.GetComponent<IStunnable>();
                    if (stunnable != null && !stunnable.ResistsStagger) stunnable.Stun(staggerSeconds);
                }
            }
            return hits;
        }

        // ---------- Teknikler ----------

        private void TryTechnique(int index)
        {
            if (index < 0 || index >= definition.Techniques.Count) return;
            var technique = definition.Techniques[index];
            if (technique == null || Time.time < _techniqueReadyAt[index]) return;
            if (!_state.TrySpendMana(technique.ManaCost)) return;

            _techniqueReadyAt[index] = Time.time + technique.Cooldown;
            Trigger("Technique");
            ActionPerformed?.Invoke(PlayerActionType.Technique);

            switch (technique.Kind)
            {
                case TechniqueKind.DashStrike:
                    _busyUntil = Time.time + 0.5f;
                    _motor.Dash(transform.forward, technique.Range, 0.2f);
                    StartCoroutine(DelayedHit(0.2f, technique, 2f, 60f));
                    break;

                case TechniqueKind.AreaKick:
                    _busyUntil = Time.time + 0.6f;
                    _motor.LockMovement(0.6f);
                    StartCoroutine(DelayedHit(0.25f, technique, technique.Range, 180f));
                    break;

                case TechniqueKind.WaterStance:
                    _waterStanceUntil = Time.time + technique.Duration;
                    _waterMultiplier = technique.CounterWindowMultiplier;
                    break;
            }
        }

        private IEnumerator DelayedHit(float delay, AbilityDefinition technique, float range, float halfAngle)
        {
            yield return new WaitForSeconds(delay);
            if (!_combatant.IsAlive) yield break;
            HitArea(null, technique.BaseDamage, 1f, range, halfAngle, 0f, technique.BreaksShield);
        }

        // ---------- Essence ----------

        private void TryEmpower()
        {
            if (IsEmpowered || Time.time < _empowerReadyAt || _state.Essences.Count == 0) return;
            _state.Essences.SetEmpowered(true, Config.empowerMultiplier);
            _empowerEndsAt = Time.time + Config.empowerDuration;
            _empowerReadyAt = _empowerEndsAt + Config.empowerCooldown;
        }

        private void UpdateEmpower()
        {
            if (IsEmpowered && Time.time >= _empowerEndsAt) _state.Essences.SetEmpowered(false, 1f);
        }

        private void TryAbsorbNearbyEssence()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, interactRadius, _interactBuffer,
                                                      ~0, QueryTriggerInteraction.Collide);
            EssencePickup nearest = null;
            float best = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var pickup = _interactBuffer[i].GetComponentInParent<EssencePickup>();
                if (pickup == null || pickup.Definition == null) continue;
                float d = (pickup.transform.position - transform.position).sqrMagnitude;
                if (d < best) { best = d; nearest = pickup; }
            }
            if (nearest != null) Absorb(nearest);
        }

        /// <summary>
        /// Essence emer. Slotlar doluysa EssenceSwapRequested ile oyuncuya hangi slotu değiştireceği sorulur.
        /// Dinleyen bir arayüz yoksa sırayla en eski slotun yerine koyar.
        /// </summary>
        public bool Absorb(EssencePickup pickup)
        {
            var data = pickup.Definition.ToData();
            var slots = _state.Essences;
            if (slots.Contains(data.Id))
            {
                BarkBus.Say(definition.DisplayName, "Bu Essence zaten bende.");
                return false;
            }

            if (!slots.IsFull) return AbsorbInto(pickup, -1);
            if (EssenceSwapRequested != null) { EssenceSwapRequested.Invoke(pickup); return false; }

            bool ok = AbsorbInto(pickup, _nextReplaceIndex);
            if (ok) _nextReplaceIndex = (_nextReplaceIndex + 1) % slots.Capacity;
            return ok;
        }

        /// <summary>Essence'ı belirli bir slota koyar (-1 = ilk boş slot).</summary>
        public bool AbsorbInto(EssencePickup pickup, int slotIndex)
        {
            if (pickup == null || pickup.Definition == null) return false;
            var data = pickup.Definition.ToData();
            if (!_state.Essences.TryAbsorb(data, slotIndex, out var replaced)) return false;

            pickup.Consume();
            EssenceAbsorbed?.Invoke(data, replaced);
            return true;
        }

        private void RefreshMoveSpeed()
        {
            _motor.SpeedMultiplier = 1f + _state.Essences.Total(EssenceBonusType.MoveSpeedPercent) / 100f;
        }

        private void Trigger(string name)
        {
            if (animator != null && animator.isActiveAndEnabled && animator.runtimeAnimatorController != null)
                animator.SetTrigger(name);
        }
    }
}
