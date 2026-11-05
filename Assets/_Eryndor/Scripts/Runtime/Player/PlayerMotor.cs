// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using UnityEngine;

namespace Eryndor.Runtime.Player
{
    /// <summary>
    /// Üçüncü şahıs hareket: kameraya göre yürüme, dönme, yerçekimi ve kaçış (dash).
    /// Kamera Cinemachine ile kurulur; bu script sadece Camera.main yönünü okur.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [SerializeField] private float turnSpeed = 720f;
        [SerializeField] private float gravity = -20f;

        private CharacterController _controller;
        private Transform _camera;
        private float _verticalVelocity;
        private float _lockedUntil;
        private Vector3 _dashVelocity;
        private float _dashUntil;

        public float BaseSpeed { get; set; } = 5f;
        /// <summary>Essence'tan gelen hız bonusu dahil çarpan.</summary>
        public float SpeedMultiplier { get; set; } = 1f;
        public bool IsDashing => Time.time < _dashUntil;
        public bool IsLocked => Time.time < _lockedUntil;
        public float CurrentSpeed01 { get; private set; }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (Camera.main != null) _camera = Camera.main.transform;
        }

        /// <summary>Saldırı sırasında yürümeyi kilitler.</summary>
        public void LockMovement(float seconds) => _lockedUntil = Mathf.Max(_lockedUntil, Time.time + seconds);

        public void Dash(Vector3 direction, float distance, float duration)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f) direction = transform.forward;
            _dashVelocity = direction.normalized * (distance / Mathf.Max(0.01f, duration));
            _dashUntil = Time.time + duration;
            transform.rotation = Quaternion.LookRotation(direction.normalized);
        }

        public void FaceTowards(Vector3 worldPoint)
        {
            Vector3 dir = worldPoint - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(dir);
        }

        /// <summary>Kameraya göre dünya yönü (girdi yoksa sıfır).</summary>
        public Vector3 CameraRelative(Vector2 input)
        {
            if (_camera == null && Camera.main != null) _camera = Camera.main.transform;
            Vector3 forward = _camera ? _camera.forward : Vector3.forward;
            Vector3 right = _camera ? _camera.right : Vector3.right;
            forward.y = 0f; right.y = 0f;
            return (forward.normalized * input.y + right.normalized * input.x);
        }

        public void Tick(Vector2 moveInput)
        {
            Vector3 horizontal;
            if (IsDashing)
            {
                horizontal = _dashVelocity;
            }
            else if (IsLocked)
            {
                horizontal = Vector3.zero;
            }
            else
            {
                Vector3 dir = CameraRelative(moveInput);
                horizontal = dir * (BaseSpeed * SpeedMultiplier);
                if (dir.sqrMagnitude > 0.001f)
                {
                    var target = Quaternion.LookRotation(dir);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);
                }
            }

            if (_controller.isGrounded && _verticalVelocity < 0f) _verticalVelocity = -2f;
            _verticalVelocity += gravity * Time.deltaTime;

            Vector3 velocity = horizontal + Vector3.up * _verticalVelocity;
            _controller.Move(velocity * Time.deltaTime);
            CurrentSpeed01 = Mathf.Clamp01(horizontal.magnitude / Mathf.Max(0.01f, BaseSpeed));
        }
    }
}
