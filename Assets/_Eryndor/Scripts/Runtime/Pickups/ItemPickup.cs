// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using Eryndor.Data;
using Eryndor.Runtime.Player;
using UnityEngine;

namespace Eryndor.Runtime.Pickups
{
    /// <summary>Üstünden geçince otomatik alınan eşya. Collider'ı "Is Trigger" olmalı.</summary>
    public sealed class ItemPickup : MonoBehaviour
    {
        [SerializeField] private ItemDefinition definition;
        [SerializeField, Min(1)] private int count = 1;

        public void Initialize(ItemDefinition item, int amount)
        {
            definition = item;
            count = Mathf.Max(1, amount);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (definition == null) return;
            var inventory = other.GetComponentInParent<PlayerInventory>();
            if (inventory == null) return;

            if (inventory.TryPickUp(definition, count, out int leftover))
            {
                if (leftover <= 0) Destroy(gameObject);
                else count = leftover;
            }
        }
    }
}
