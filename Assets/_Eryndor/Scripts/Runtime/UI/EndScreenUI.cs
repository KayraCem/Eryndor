// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using Eryndor.Core.Combat;
using Eryndor.Runtime.Game;
using TMPro;
using UnityEngine;

namespace Eryndor.Runtime.UI
{
    /// <summary>Zafer / ölüm ekranı ve koşu özeti (GDD 9). Butonlar Restart() ve QuitToMenu()'ya bağlanır.</summary>
    public sealed class EndScreenUI : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text statsLabel;
        [SerializeField] private GameObject pausePanel;

        private void Start()
        {
            if (panel != null) panel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);
            var gm = GameManager.Instance;
            if (gm == null) return;
            gm.RunEnded += Show;
            gm.PauseChanged += paused => { if (pausePanel != null) pausePanel.SetActive(paused); };
        }

        private void Show(GameManager.RunResult result, RunStats stats)
        {
            if (panel != null) panel.SetActive(true);
            if (titleLabel != null)
                titleLabel.text = result == GameManager.RunResult.Won ? "Bekçi düştü. Kapı açıldı." : "Kuyu seni yuttu.";
            if (statsLabel != null)
                statsLabel.text =
                    "Süre: " + stats.FormatTime() +
                    "\nÖldürülen canavar: " + stats.Kills +
                    "\nBaşarılı counter: " + stats.SuccessfulCounters +
                    "\nEmilen Essence: " + stats.EssencesAbsorbed;
        }

        public void Restart() => GameManager.Instance?.Restart();
        public void QuitToMenu() => GameManager.Instance?.QuitToMenu();
        public void Resume() => GameManager.Instance?.SetPaused(false);
    }
}
