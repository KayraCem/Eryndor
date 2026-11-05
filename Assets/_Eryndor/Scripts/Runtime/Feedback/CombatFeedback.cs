// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using System.Collections;
using System.Collections.Generic;
using Eryndor.Core.Combat;
using Eryndor.Runtime.Boss;
using Eryndor.Runtime.Combat;
using Eryndor.Runtime.Enemies;
using Eryndor.Runtime.Player;
using UnityEngine;

namespace Eryndor.Runtime.Feedback
{
    /// <summary>
    /// Dövüş hissi (GDD 8: "counter'ın en önemli geri bildirimi"):
    ///  - Her isabette kısa hit-stop, hasar sayısı, beyaz parlama ve darbe sesi
    ///  - Başarılı counter'da "çın" sesi, yavaş çekim ve güçlü kamera sarsıntısı
    ///  - Düşman saldırı uyarısında ses: tiz = counter'la, pes = kaç
    /// Sadece olayları dinler; dövüş mantığına dokunmaz.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class CombatFeedback : MonoBehaviour
    {
        [Header("Zaman")]
        [SerializeField] private float hitStopSeconds = 0.05f;
        [SerializeField] private float counterSlowMoScale = 0.25f;
        [SerializeField] private float counterSlowMoSeconds = 0.35f;

        [Header("Kamera")]
        [SerializeField] private float hitShake = 0.08f;
        [SerializeField] private float counterShake = 0.3f;
        [SerializeField] private float hurtShake = 0.18f;

        [Header("Sesler (boşsa kodla üretilir)")]
        [SerializeField] private AudioClip counterClip;
        [SerializeField] private AudioClip punchClip;
        [SerializeField] private AudioClip hurtClip;
        [SerializeField] private AudioClip warnCounterableClip;
        [SerializeField] private AudioClip warnDangerClip;

        private static readonly Color CounterColor = new Color(0.45f, 0.85f, 1f);
        private static readonly Color HurtColor = new Color(1f, 0.3f, 0.25f);
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private AudioSource _audio;
        private ThirdPersonCamera _camera;
        private Combatant _player;
        private bool _ownsTimeScale;
        private float _timeEffectUntil;
        private readonly Dictionary<Combatant, Renderer[]> _renderers = new Dictionary<Combatant, Renderer[]>();
        private MaterialPropertyBlock _flashBlock;

        private void Awake()
        {
            _audio = GetComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f;
            _flashBlock = new MaterialPropertyBlock();
            _flashBlock.SetColor(BaseColorId, Color.white);

            if (counterClip == null) counterClip = ProceduralSfx.CounterRing();
            if (punchClip == null) punchClip = ProceduralSfx.Punch();
            if (hurtClip == null) hurtClip = ProceduralSfx.Hurt();
            if (warnCounterableClip == null) warnCounterableClip = ProceduralSfx.WarningCounterable();
            if (warnDangerClip == null) warnDangerClip = ProceduralSfx.WarningDanger();
        }

        private void Start()
        {
            if (Camera.main != null) _camera = Camera.main.GetComponent<ThirdPersonCamera>();

            foreach (var c in FindObjectsByType<Combatant>(FindObjectsSortMode.None))
            {
                var combatant = c;
                combatant.Damaged += (hit, amount) => OnDamaged(combatant, hit, amount);
                if (combatant.Team == Team.Player)
                {
                    _player = combatant;
                    var pc = combatant.GetComponent<PlayerCombat>();
                    if (pc != null) pc.CounterSucceeded += _ => OnCounter();
                }
            }

            foreach (var e in FindObjectsByType<EnemyController>(FindObjectsSortMode.None)) e.Telegraphed += OnTelegraph;
            foreach (var b in FindObjectsByType<BossController>(FindObjectsSortMode.None)) b.Telegraphed += OnTelegraph;
        }

        private void OnDamaged(Combatant target, HitInfo hit, float amount)
        {
            Vector3 top = target.AimPosition + Vector3.up * 1.1f;
            bool isCounterStrike = hit.attack != null && hit.attack.id == "counter";

            if (target == _player)
            {
                FloatingText.Spawn(top, Mathf.RoundToInt(amount).ToString(), HurtColor, 5f);
                Play(hurtClip, 0.9f);
                Shake(hurtShake);
                return;
            }

            FloatingText.Spawn(top, Mathf.RoundToInt(amount).ToString(),
                               isCounterStrike ? CounterColor : Color.white, isCounterStrike ? 8f : 5f);
            Play(punchClip, 0.8f, Random.Range(0.9f, 1.1f));
            Flash(target);
            if (!isCounterStrike)
            {
                Shake(hitShake);
                TimeEffect(0.02f, hitStopSeconds);
            }
        }

        private void OnCounter()
        {
            Play(counterClip, 1f);
            Shake(counterShake);
            TimeEffect(counterSlowMoScale, counterSlowMoSeconds);
        }

        private void OnTelegraph(AttackData attack)
        {
            if (attack == null) return;
            Play(attack.counterable ? warnCounterableClip : warnDangerClip, 0.6f);
        }

        // ---------- Yardımcılar ----------

        private void Play(AudioClip clip, float volume, float pitch = 1f)
        {
            if (clip == null) return;
            _audio.pitch = pitch;
            _audio.PlayOneShot(clip, volume);
        }

        private void Shake(float amount)
        {
            if (_camera != null) _camera.AddShake(amount);
        }

        /// <summary>Zamanı kısa süre yavaşlatır. Oyun duraklatıldıysa ya da bir menü açıksa dokunmaz.</summary>
        private void TimeEffect(float scale, float realSeconds)
        {
            if (!_ownsTimeScale && Time.timeScale < 0.999f) return;
            _timeEffectUntil = Mathf.Max(_timeEffectUntil, Time.unscaledTime + realSeconds);
            if (_ownsTimeScale && Time.timeScale <= scale) return;
            Time.timeScale = scale;
            if (!_ownsTimeScale)
            {
                _ownsTimeScale = true;
                StartCoroutine(RestoreTime());
            }
        }

        private IEnumerator RestoreTime()
        {
            while (Time.unscaledTime < _timeEffectUntil) yield return null;
            if (_ownsTimeScale && Time.timeScale > 0f) Time.timeScale = 1f; // duraklatma (0) varsa ona dokunma
            _ownsTimeScale = false;
        }

        private void Flash(Combatant target)
        {
            if (!_renderers.TryGetValue(target, out var renderers))
            {
                renderers = target.GetComponentsInChildren<Renderer>();
                _renderers[target] = renderers;
            }
            StartCoroutine(FlashRoutine(renderers));
        }

        private IEnumerator FlashRoutine(Renderer[] renderers)
        {
            foreach (var r in renderers) if (r != null) r.SetPropertyBlock(_flashBlock);
            yield return new WaitForSecondsRealtime(0.07f);
            foreach (var r in renderers) if (r != null) r.SetPropertyBlock(null);
        }

        private void OnDisable()
        {
            if (_ownsTimeScale && Time.timeScale > 0f) Time.timeScale = 1f;
            _ownsTimeScale = false;
        }
    }
}
