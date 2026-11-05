// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using Eryndor.Data;
using Eryndor.Runtime.UI;
using UnityEngine;

namespace Eryndor.Runtime.Pickups
{
    /// <summary>Yerdeki emilebilir Essence. Oyuncu yaklaşıp F'ye basınca emilir.</summary>
    public sealed class EssencePickup : MonoBehaviour
    {
        [SerializeField] private EssenceDefinition definition;
        [SerializeField] private float bobHeight = 0.2f;
        [SerializeField] private float spinSpeed = 90f;

        [SerializeField] private float promptRadius = 2.5f;

        private Vector3 _startPosition;
        private Transform _player;

        public EssenceDefinition Definition => definition;

        public void Initialize(EssenceDefinition essence) => definition = essence;

        private void Start()
        {
            _startPosition = transform.position;
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) _player = go.transform;
        }

        private void Update()
        {
            if (_player != null && definition != null)
            {
                bool near = (_player.position - transform.position).sqrMagnitude <= promptRadius * promptRadius;
                if (near) InteractionPrompt.Set(this, "<b>F</b>  Em: " + definition.DisplayName);
                else InteractionPrompt.Clear(this);
            }

            transform.position = _startPosition + Vector3.up * (Mathf.Sin(Time.time * 2f) * bobHeight);
            transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
        }

        public void Consume() => Destroy(gameObject);

        private void OnDestroy() => InteractionPrompt.Clear(this);
    }
}
