// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using System.Text;
using Eryndor.Core.Items;
using Eryndor.Runtime.Pickups;
using Eryndor.Runtime.Player;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Eryndor.Runtime.UI
{
    /// <summary>
    /// Slotlar doluyken yeni bir Essence emilmek istenince açılır (GDD 4.5).
    /// Oyun durur; oyuncu 1-3 ile değiştireceği slotu seçer ya da Esc ile vazgeçer.
    /// </summary>
    public sealed class EssenceSwapUI : MonoBehaviour
    {
        [SerializeField] private PlayerCombat player;
        [SerializeField] private GameObject panel;
        [SerializeField] private TMP_Text body;

        private EssencePickup _pending;

        public bool IsOpen => _pending != null;

        private void Start()
        {
            if (panel != null) panel.SetActive(false);
            if (player != null) player.EssenceSwapRequested += Open;
        }

        private void OnDestroy()
        {
            if (player != null) player.EssenceSwapRequested -= Open;
            if (IsOpen) Close();
        }

        private void Open(EssencePickup pickup)
        {
            if (IsOpen || pickup == null) return;
            _pending = pickup;
            UIModal.Open();
            Time.timeScale = 0f;
            if (panel != null) panel.SetActive(true);
            if (body != null) body.text = BuildText(pickup);
        }

        private void Close()
        {
            _pending = null;
            UIModal.Close();
            Time.timeScale = 1f;
            if (panel != null) panel.SetActive(false);
        }

        private void Update()
        {
            if (!IsOpen) return;
            if (_pending == null || _pending.Definition == null) { Close(); return; }

            var kb = Keyboard.current;
            var pad = Gamepad.current;
            int choice = -1;
            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame) choice = 0;
                else if (kb.digit2Key.wasPressedThisFrame) choice = 1;
                else if (kb.digit3Key.wasPressedThisFrame) choice = 2;
            }
            if (pad != null)
            {
                if (pad.dpad.left.wasPressedThisFrame) choice = 0;
                else if (pad.dpad.up.wasPressedThisFrame) choice = 1;
                else if (pad.dpad.right.wasPressedThisFrame) choice = 2;
            }

            if (choice >= 0 && choice < player.State.Essences.Capacity)
            {
                player.AbsorbInto(_pending, choice);
                Close();
                return;
            }

            bool cancel = (kb != null && kb.escapeKey.wasPressedThisFrame) || (pad != null && pad.buttonEast.wasPressedThisFrame);
            if (cancel) Close();
        }

        private string BuildText(EssencePickup pickup)
        {
            var def = pickup.Definition;
            var sb = new StringBuilder();
            sb.AppendLine("<size=120%><b>Essence slotların dolu</b></size>");
            sb.AppendLine();
            sb.Append("Yeni: <color=#7FC4FF><b>").Append(def.DisplayName).Append("</b></color>  <size=80%>(")
              .Append(def.Rarity.ToTurkish()).AppendLine(")</size>");
            if (!string.IsNullOrEmpty(def.Description)) sb.Append("<size=85%>").Append(def.Description).AppendLine("</size>");
            sb.AppendLine();

            var slots = player.State.Essences;
            for (int i = 0; i < slots.Capacity; i++)
            {
                var e = slots.Get(i);
                sb.Append("<b>").Append(i + 1).Append("</b>  ").AppendLine(e == null ? "—" : e.DisplayName);
            }
            sb.AppendLine();
            sb.Append("<size=85%>1-3: o slotla değiştir     Esc: vazgeç</size>");
            return sb.ToString();
        }
    }
}
