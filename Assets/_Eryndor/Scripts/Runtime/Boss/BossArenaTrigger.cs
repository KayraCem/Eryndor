// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using UnityEngine;

namespace Eryndor.Runtime.Boss
{
    /// <summary>Oyuncu Bekçi Salonu'na girince boss'u uyandırır. Collider'ı "Is Trigger" olmalı.</summary>
    [RequireComponent(typeof(Collider))]
    public sealed class BossArenaTrigger : MonoBehaviour
    {
        [SerializeField] private BossController boss;
        [Tooltip("Oyuncu girince kapanacak kapı (isteğe bağlı).")]
        [SerializeField] private GameObject entranceBlocker;

        private void OnTriggerEnter(Collider other)
        {
            if (boss == null || !other.CompareTag("Player")) return;
            boss.Activate();
            if (entranceBlocker != null) entranceBlocker.SetActive(true);
            enabled = false;
        }
    }
}
