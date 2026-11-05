// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using Eryndor.Runtime.Combat;
using Eryndor.Runtime.Player;
using Eryndor.Runtime.UI;
using UnityEngine;

namespace Eryndor.Runtime.World
{
    /// <summary>
    /// Dinlenme Kampı'ndaki sunak (GDD 7, oda 3). F ile dinlenince can ve Stamina tamamen dolar.
    /// Varsayılan olarak koşu başına bir kez kullanılabilir.
    /// </summary>
    public sealed class RestPoint : MonoBehaviour
    {
        [SerializeField] private float radius = 2.8f;
        [SerializeField] private bool singleUse = true;
        [SerializeField] private Light flame;

        private Transform _player;
        private PlayerCombat _combat;
        private PlayerInputReader _input;
        private bool _used;

        private void Start()
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go == null) return;
            _player = go.transform;
            _combat = go.GetComponent<PlayerCombat>();
            _input = go.GetComponent<PlayerInputReader>();
        }

        private void Update()
        {
            if (_player == null || _combat == null || _combat.State == null) return;
            bool available = !(_used && singleUse) && !_combat.State.IsDead;
            bool near = (_player.position - transform.position).sqrMagnitude <= radius * radius;

            if (!available || !near) { InteractionPrompt.Clear(this); return; }
            InteractionPrompt.Set(this, "<b>F</b>  Dinlen (can yenilenir)");

            if (_input != null && _input.InteractPressed) Rest();
        }

        private void Rest()
        {
            var s = _combat.State;
            s.Heal(s.MaxHp);
            s.RegenerateStamina(999f);
            _used = true;
            InteractionPrompt.Clear(this);
            BarkBus.Say(_combat.Definition.DisplayName, "Biraz nefes... Bekçi beni bekliyor.");
            if (flame != null && singleUse) flame.intensity *= 0.35f;
        }

        private void OnDisable() => InteractionPrompt.Clear(this);
    }
}
