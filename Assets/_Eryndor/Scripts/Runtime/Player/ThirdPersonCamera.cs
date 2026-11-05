// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using Eryndor.Runtime.Game;
using Eryndor.Runtime.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Eryndor.Runtime.Player
{
    /// <summary>
    /// Basit üçüncü şahıs kamera: fare / sağ analog ile oyuncunun etrafında döner,
    /// duvara çarpınca yaklaşır. Cinemachine'e bağımlı değil; istenirse sonra Cinemachine'e geçilebilir.
    /// </summary>
    public sealed class ThirdPersonCamera : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float distance = 6f;
        [SerializeField] private float focusHeight = 1.6f;
        [SerializeField] private float mouseSensitivity = 0.12f;
        [SerializeField] private float stickSensitivity = 160f;
        [SerializeField] private float minPitch = -15f;
        [SerializeField] private float maxPitch = 65f;
        [Tooltip("Kameranın içinden geçemeyeceği katmanlar (duvarlar).")]
        [SerializeField] private LayerMask collisionMask;

        private float _yaw;
        private float _pitch = 20f;
        private float _shake;

        public void SetTarget(Transform newTarget) => target = newTarget;

        /// <summary>Kamera sarsıntısı ekler (metre). Zamanla söner.</summary>
        public void AddShake(float amount) => _shake = Mathf.Max(_shake, amount);

        private void Start()
        {
            if (target != null) _yaw = target.eulerAngles.y;
        }

        private void LateUpdate()
        {
            bool playing = Time.timeScale > 0f && !UIModal.IsOpen &&
                           (GameManager.Instance == null || GameManager.Instance.Result == GameManager.RunResult.Playing);
            Cursor.lockState = playing ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !playing;
            if (target == null) return;

            if (playing)
            {
                Vector2 look = Vector2.zero;
                if (Mouse.current != null) look += Mouse.current.delta.ReadValue() * mouseSensitivity;
                if (Gamepad.current != null) look += Gamepad.current.rightStick.ReadValue() * (stickSensitivity * Time.deltaTime);
                _yaw += look.x;
                _pitch = Mathf.Clamp(_pitch - look.y, minPitch, maxPitch);
            }

            Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 focus = target.position + Vector3.up * focusHeight;
            Vector3 back = rotation * Vector3.back;
            float d = distance;
            if (Physics.SphereCast(focus, 0.25f, back, out RaycastHit hit, distance, collisionMask, QueryTriggerInteraction.Ignore))
                d = Mathf.Max(0.5f, hit.distance);

            Vector3 shakeOffset = Vector3.zero;
            if (_shake > 0.001f)
            {
                shakeOffset = Random.insideUnitSphere * _shake;
                _shake = Mathf.MoveTowards(_shake, 0f, Time.unscaledDeltaTime * 1.5f);
            }

            transform.position = focus + back * d + shakeOffset;
            transform.rotation = rotation;
        }
    }
}
