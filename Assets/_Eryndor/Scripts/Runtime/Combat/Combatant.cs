// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using System;
using Eryndor.Core.Characters;
using Eryndor.Core.Combat;
using UnityEngine;

namespace Eryndor.Runtime.Combat
{
    public enum Team { Player, Enemy }

    public enum HitOutcome { Ignored, Hit, Countered, Killed }

    /// <summary>Bir vuruşun bilgisi. damage, saldıranın STR çarpanı uygulanmış hasardır.</summary>
    public struct HitInfo
    {
        public Combatant source;
        public AttackData attack;
        public float damage;
        public bool counterable;

        public HitInfo(Combatant source, AttackData attack, float damage, bool counterable)
        {
            this.source = source;
            this.attack = attack;
            this.damage = damage;
            this.counterable = counterable;
        }
    }

    /// <summary>Sersemletilebilen her şey (counter ya da ağır vuruş sonrası).</summary>
    public interface IStunnable
    {
        /// <summary>true ise ağır vuruşla sersemlemez (boss); counter yine sersemletir.</summary>
        bool ResistsStagger { get; }
        void Stun(float seconds);
    }

    /// <summary>Kalkanı/savunma duruşu kırılabilen dövüşçü (Demir Avuç tekniği).</summary>
    public interface IGuardable
    {
        void BreakGuard();
    }

    /// <summary>Duman bombası: tüm düşmanlar oyuncuyu bir süre kaybeder.</summary>
    public static class SmokeBomb
    {
        public static event System.Action<float> Detonated;
        public static void Detonate(float seconds) => Detonated?.Invoke(seconds);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Detonated = null;
    }

    /// <summary>
    /// Sahnedeki her dövüşçünün ortak bileşeni. CharacterState'i (runtime durum) taşır ve
    /// gelen vuruşları karşılar. Oyuncu tarafında CounterInterceptor, counter kontrolünü yapar.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Combatant : MonoBehaviour
    {
        [SerializeField] private Team team = Team.Enemy;
        [Tooltip("Vuruşların hedef aldığı nokta (göğüs hizası). Boşsa transform kullanılır.")]
        [SerializeField] private Transform aimPoint;

        public Team Team => team;
        public CharacterState State { get; private set; }
        public bool IsAlive => State != null && !State.IsDead;
        public Vector3 AimPosition => aimPoint ? aimPoint.position : transform.position + Vector3.up;

        /// <summary>Alınan hasar çarpanı (ör. boss savunma duruşunda 0,5).</summary>
        public float DamageTakenMultiplier { get; set; } = 1f;

        /// <summary>true dönerse vuruş counter'lanmıştır ve hasar uygulanmaz.</summary>
        public Func<HitInfo, bool> CounterInterceptor { get; set; }

        public event Action<HitInfo, float> Damaged;      // (vuruş, uygulanan hasar)
        public event Action<HitInfo> CounteredIncoming;   // bu dövüşçü bir saldırıyı counter'ladı
        public event Action<Combatant> Died;

        public void Initialize(CharacterState state)
        {
            if (State != null) State.Died -= OnStateDied;
            State = state ?? throw new ArgumentNullException(nameof(state));
            State.Died += OnStateDied;
        }

        public HitOutcome ReceiveHit(HitInfo hit)
        {
            if (!IsAlive) return HitOutcome.Ignored;

            if (CounterInterceptor != null && CounterInterceptor(hit))
            {
                CounteredIncoming?.Invoke(hit);
                return HitOutcome.Countered;
            }

            float damage = Mathf.Max(0f, hit.damage - State.Defense) * DamageTakenMultiplier;
            float applied = State.TakeDamage(damage, Time.time);
            if (applied <= 0f) return HitOutcome.Ignored;

            Damaged?.Invoke(hit, applied);
            return State.IsDead ? HitOutcome.Killed : HitOutcome.Hit;
        }

        private void OnStateDied() => Died?.Invoke(this);

        private void OnDestroy()
        {
            if (State != null) State.Died -= OnStateDied;
        }
    }
}
