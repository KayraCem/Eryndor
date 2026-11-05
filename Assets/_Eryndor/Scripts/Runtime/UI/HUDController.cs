// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using Eryndor.Core.Essence;
using Eryndor.Core.Items;
using Eryndor.Runtime.Boss;
using Eryndor.Runtime.Combat;
using Eryndor.Runtime.Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Eryndor.Runtime.UI
{
    /// <summary>
    /// HUD (GDD 9): can/Mana/Stamina, Essence slotları, Güçlendirme bekleme süresi,
    /// envanter, boss can çubuğu ve bark altyazısı. Sadece olayları dinler, oyun mantığı içermez.
    /// </summary>
    public sealed class HUDController : MonoBehaviour
    {
        [Header("Kaynaklar")]
        [SerializeField] private PlayerCombat player;
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private BossController boss;

        [Header("Çubuklar")]
        [SerializeField] private Slider healthBar;
        [SerializeField] private Slider manaBar;
        [SerializeField] private Slider staminaBar;

        [Header("Essence")]
        [SerializeField] private TMP_Text[] essenceSlotLabels = new TMP_Text[3];
        [SerializeField] private Image empowerCooldownFill;

        [Header("Envanter")]
        [SerializeField] private TMP_Text[] inventoryLabels = new TMP_Text[4];

        [Header("Boss")]
        [SerializeField] private GameObject bossPanel;
        [SerializeField] private Slider bossHealthBar;
        [SerializeField] private TMP_Text bossNameLabel;

        [Header("Etkileşim ipucu")]
        [SerializeField] private TMP_Text promptLabel;

        [Header("Bark")]
        [SerializeField] private TMP_Text barkLabel;
        [SerializeField] private float barkDuration = 2.5f;

        private float _barkHideAt;

        private void OnEnable()
        {
            BarkBus.Spoken += ShowBark;
            InteractionPrompt.Changed += ShowPrompt;
        }

        private void OnDisable()
        {
            BarkBus.Spoken -= ShowBark;
            InteractionPrompt.Changed -= ShowPrompt;
        }

        private void ShowPrompt(string text)
        {
            if (promptLabel != null) promptLabel.text = text;
        }

        private void Start()
        {
            if (bossPanel != null) bossPanel.SetActive(false);
            if (barkLabel != null) barkLabel.text = string.Empty;
            ShowPrompt(InteractionPrompt.Current);

            if (player != null && player.State != null)
            {
                var s = player.State;
                s.HealthChanged += (cur, max) => SetBar(healthBar, cur, max);
                s.ManaChanged += (cur, max) => SetBar(manaBar, cur, max);
                s.StaminaChanged += (cur, max) => SetBar(staminaBar, cur, max);
                s.Essences.Changed += RefreshEssences;
                SetBar(healthBar, s.CurrentHp, s.MaxHp);
                SetBar(manaBar, s.CurrentMana, s.MaxMana);
                SetBar(staminaBar, s.CurrentStamina, s.MaxStamina);
                RefreshEssences();
            }

            if (inventory != null && inventory.Inventory != null)
            {
                inventory.Inventory.Changed += RefreshInventory;
                RefreshInventory();
            }

            if (boss != null)
            {
                boss.Activated += ShowBoss;
                boss.Combatant.Died += _ => { if (bossPanel != null) bossPanel.SetActive(false); };
            }
        }

        private void Update()
        {
            if (empowerCooldownFill != null && player != null) empowerCooldownFill.fillAmount = player.EmpowerCooldown01;
            if (barkLabel != null && barkLabel.text.Length > 0 && Time.time >= _barkHideAt) barkLabel.text = string.Empty;
        }

        private void ShowBoss()
        {
            if (bossPanel != null) bossPanel.SetActive(true);
            if (bossNameLabel != null) bossNameLabel.text = boss.Definition.DisplayName;
            var s = boss.Combatant.State;
            s.HealthChanged += (cur, max) => SetBar(bossHealthBar, cur, max);
            SetBar(bossHealthBar, s.CurrentHp, s.MaxHp);
        }

        private void RefreshEssences()
        {
            var slots = player.State.Essences;
            for (int i = 0; i < essenceSlotLabels.Length; i++)
            {
                if (essenceSlotLabels[i] == null) continue;
                EssenceData e = i < slots.Capacity ? slots.Get(i) : null;
                essenceSlotLabels[i].text = e == null ? "—" : $"{e.DisplayName}\n<size=70%>{e.Rarity.ToTurkish()}</size>";
                essenceSlotLabels[i].color = slots.IsEmpowered ? new Color(0.55f, 0.8f, 1f) : Color.white;
            }
        }

        private void RefreshInventory()
        {
            var inv = inventory.Inventory;
            for (int i = 0; i < inventoryLabels.Length; i++)
            {
                if (inventoryLabels[i] == null) continue;
                if (i >= inv.Capacity) { inventoryLabels[i].text = string.Empty; continue; }
                var slot = inv.GetSlot(i);
                inventoryLabels[i].text = slot.IsEmpty ? $"{i + 1}" : $"{i + 1}  {slot.Item.DisplayName} x{slot.Count}";
            }
        }

        private void ShowBark(string speaker, string line)
        {
            if (barkLabel == null) return;
            barkLabel.text = $"<b>{speaker}:</b> {line}";
            _barkHideAt = Time.time + barkDuration;
        }

        private static void SetBar(Slider bar, float current, float max)
        {
            if (bar == null) return;
            bar.maxValue = Mathf.Max(1f, max);
            bar.value = current;
        }
    }
}
