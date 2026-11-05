// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Eryndor.Runtime.UI
{
    /// <summary>Ana menü: Başla ve Çıkış butonları.</summary>
    public sealed class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private string gameScene = "TestArena";

        private void Start()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Play() => SceneManager.LoadScene(gameScene);

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
