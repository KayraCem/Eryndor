// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using Eryndor.Runtime.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Eryndor.Runtime.Player
{
    /// <summary>
    /// Girdileri tek yerde okur (Input System, klavye + fare + gamepad).
    /// Diğer scriptler tuşlara değil bu özelliklere bakar; kontroller değişirse sadece burası değişir.
    /// Kontroller GDD Bölüm 3'teki tabloya göredir.
    /// </summary>
    public sealed class PlayerInputReader : MonoBehaviour
    {
        public Vector2 Move { get; private set; }
        public bool LightPressed { get; private set; }
        public bool HeavyPressed { get; private set; }
        public bool CounterPressed { get; private set; }
        public bool DodgePressed { get; private set; }
        public bool Technique1Pressed { get; private set; }
        public bool Technique2Pressed { get; private set; }
        public bool EmpowerHeld { get; private set; }
        public bool InteractPressed { get; private set; }
        public bool PausePressed { get; private set; }
        /// <summary>Basılan envanter tuşu (0-3), yoksa -1.</summary>
        public int UseItemSlot { get; private set; } = -1;

        private void Update()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            var pad = Gamepad.current;

            Vector2 move = Vector2.zero;
            if (kb != null)
            {
                if (kb.wKey.isPressed) move.y += 1f;
                if (kb.sKey.isPressed) move.y -= 1f;
                if (kb.dKey.isPressed) move.x += 1f;
                if (kb.aKey.isPressed) move.x -= 1f;
            }
            if (pad != null && pad.leftStick.ReadValue().sqrMagnitude > move.sqrMagnitude)
                move = pad.leftStick.ReadValue();
            Move = Vector2.ClampMagnitude(move, 1f);

            bool padLb = pad != null && pad.leftShoulder.isPressed;

            LightPressed = (mouse != null && mouse.leftButton.wasPressedThisFrame) || (pad != null && !padLb && pad.buttonWest.wasPressedThisFrame);
            HeavyPressed = (mouse != null && mouse.rightButton.wasPressedThisFrame) || (pad != null && !padLb && pad.buttonNorth.wasPressedThisFrame);
            CounterPressed = (kb != null && kb.spaceKey.wasPressedThisFrame) || (pad != null && pad.rightShoulder.wasPressedThisFrame);
            DodgePressed = (kb != null && kb.leftShiftKey.wasPressedThisFrame) || (pad != null && pad.buttonEast.wasPressedThisFrame);
            Technique1Pressed = (kb != null && kb.qKey.wasPressedThisFrame) || (pad != null && padLb && pad.buttonWest.wasPressedThisFrame);
            Technique2Pressed = (kb != null && kb.eKey.wasPressedThisFrame) || (pad != null && padLb && pad.buttonNorth.wasPressedThisFrame);
            EmpowerHeld = (kb != null && kb.rKey.isPressed) || (pad != null && pad.leftTrigger.ReadValue() > 0.5f);
            InteractPressed = (kb != null && kb.fKey.wasPressedThisFrame) || (pad != null && pad.buttonSouth.wasPressedThisFrame);
            PausePressed = (kb != null && kb.escapeKey.wasPressedThisFrame) || (pad != null && pad.startButton.wasPressedThisFrame);

            UseItemSlot = -1;
            if (kb != null)
            {
                if (kb.digit1Key.wasPressedThisFrame) UseItemSlot = 0;
                else if (kb.digit2Key.wasPressedThisFrame) UseItemSlot = 1;
                else if (kb.digit3Key.wasPressedThisFrame) UseItemSlot = 2;
                else if (kb.digit4Key.wasPressedThisFrame) UseItemSlot = 3;
            }

            if (UIModal.IsOpen) Clear(); // menü açıkken karakter hareket etmez
        }

        private void Clear()
        {
            Move = Vector2.zero;
            LightPressed = HeavyPressed = CounterPressed = DodgePressed = false;
            Technique1Pressed = Technique2Pressed = EmpowerHeld = InteractPressed = PausePressed = false;
            UseItemSlot = -1;
        }
    }
}
