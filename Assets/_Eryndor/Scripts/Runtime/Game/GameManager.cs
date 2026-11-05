// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using System;
using System.Collections;
using Eryndor.Core.Combat;
using Eryndor.Runtime.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Eryndor.Runtime.Game
{
    /// <summary>
    /// Koşunun akışı: istatistikler, duraklatma, kazanma (boss öldü) ve kaybetme (oyuncu öldü).
    /// Sahnede bir tane olur.
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        public enum RunResult { Playing, Won, Lost }

        [SerializeField] private PlayerCombat player;
        [SerializeField] private float endScreenDelay = 1.5f;
        [SerializeField] private string mainMenuScene = "MainMenu";

        public static GameManager Instance { get; private set; }

        public RunStats Stats { get; } = new RunStats();
        public RunResult Result { get; private set; } = RunResult.Playing;
        public bool IsPaused { get; private set; }

        /// <summary>Koşu bitti; UI özet ekranını gösterir.</summary>
        public event Action<RunResult, RunStats> RunEnded;
        public event Action<bool> PauseChanged;

        private PlayerInputReader _input;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Time.timeScale = 1f;
        }

        private void Start()
        {
            if (player == null)
            {
                var go = GameObject.FindGameObjectWithTag("Player");
                if (go != null) player = go.GetComponent<PlayerCombat>();
            }
            if (player == null) { Debug.LogError("[GameManager] Oyuncu bulunamadı (Player etiketi?).", this); return; }

            _input = player.GetComponent<PlayerInputReader>();
            player.Combatant.Died += _ => EndRun(RunResult.Lost);
            player.CounterSucceeded += _ => Stats.AddCounter();
            player.EssenceAbsorbed += (_, __) => Stats.AddEssence();
        }

        private void Update()
        {
            if (Result == RunResult.Playing && !IsPaused) Stats.Tick(Time.deltaTime);
            if (_input != null && _input.PausePressed && Result == RunResult.Playing) SetPaused(!IsPaused);
        }

        public void OnBossDefeated()
        {
            Stats.AddKill();
            EndRun(RunResult.Won);
        }

        public void SetPaused(bool paused)
        {
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            PauseChanged?.Invoke(paused);
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void QuitToMenu()
        {
            Time.timeScale = 1f;
            if (Application.CanStreamedLevelBeLoaded(mainMenuScene)) SceneManager.LoadScene(mainMenuScene);
            else Restart();
        }

        private void EndRun(RunResult result)
        {
            if (Result != RunResult.Playing) return;
            Result = result;
            StartCoroutine(ShowEndAfterDelay());
        }

        private IEnumerator ShowEndAfterDelay()
        {
            yield return new WaitForSeconds(endScreenDelay);
            RunEnded?.Invoke(Result, Stats);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;
    }
}
