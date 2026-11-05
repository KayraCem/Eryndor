// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using Eryndor.Core.Combat;
using Eryndor.Runtime.Enemies;
using TMPro;
using UnityEngine;

namespace Eryndor.Runtime.UI
{
    /// <summary>
    /// Düşmanın başının üstündeki gösterge (GDD 9): "?" şüphe, "!" kovalama,
    /// saldırı uyarısında sarı (counter'lanabilir) ya da kırmızı (kaç).
    /// World-space bir TMP_Text'e eklenir.
    /// </summary>
    public sealed class EnemyIndicator : MonoBehaviour
    {
        [SerializeField] private EnemyController enemy;
        [SerializeField] private TMP_Text label;
        [SerializeField] private Color counterableColor = new Color(1f, 0.85f, 0.2f);
        [SerializeField] private Color dangerColor = new Color(1f, 0.25f, 0.2f);

        private Camera _camera;

        private void Awake()
        {
            _camera = Camera.main;
            if (label == null) label = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            if (enemy == null) return;
            enemy.StateNameChanged += OnStateChanged;
            enemy.Telegraphed += OnTelegraph;
        }

        private void OnDisable()
        {
            if (enemy == null) return;
            enemy.StateNameChanged -= OnStateChanged;
            enemy.Telegraphed -= OnTelegraph;
        }

        private void LateUpdate()
        {
            if (_camera != null) transform.rotation = _camera.transform.rotation; // hep kameraya baksın
        }

        private void OnStateChanged(string state)
        {
            if (label == null) return;
            label.color = Color.white;
            switch (state)
            {
                case "Suspicious": label.text = "?"; break;
                case "Chase": label.text = "!"; break;
                case "Windup": break; // OnTelegraph belirler
                case "Stunned": label.text = "*"; break;
                default: label.text = string.Empty; break;
            }
        }

        private void OnTelegraph(AttackData attack)
        {
            if (label == null || attack == null) return;
            label.text = "!";
            label.color = attack.counterable ? counterableColor : dangerColor;
        }
    }
}
