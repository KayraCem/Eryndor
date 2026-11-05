// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using System;
using System.Collections.Generic;
using Eryndor.Core.Items;
using Eryndor.Data;
using Eryndor.Runtime.Combat;
using UnityEngine;

namespace Eryndor.Runtime.Player
{
    /// <summary>Tüketilebilir envanter (GDD 4.6). 1-4 tuşlarıyla kullanılır.</summary>
    [RequireComponent(typeof(PlayerCombat), typeof(PlayerInputReader))]
    public sealed class PlayerInventory : MonoBehaviour
    {
        [Serializable]
        private struct StartingItem
        {
            public ItemDefinition item;
            [Min(1)] public int count;
        }

        [SerializeField] private List<StartingItem> startingItems = new List<StartingItem>();

        private PlayerCombat _combat;
        private PlayerInputReader _input;

        public Inventory Inventory { get; private set; }

        private void Awake()
        {
            _combat = GetComponent<PlayerCombat>();
            _input = GetComponent<PlayerInputReader>();
            int capacity = _combat.Definition != null ? _combat.Definition.InventorySlots : 4;
            Inventory = new Inventory(capacity);
            foreach (var s in startingItems)
                if (s.item != null) Inventory.Add(s.item.ToData(), s.count);
        }

        private void Update()
        {
            if (_input.UseItemSlot >= 0) Use(_input.UseItemSlot);
        }

        public void Use(int slotIndex)
        {
            if (_combat.State == null || _combat.State.IsDead) return;
            var item = Inventory.ConsumeOne(slotIndex);
            if (item == null) return;

            if (!item.ApplyTo(_combat.State) && item.Effect == ConsumableEffect.Smoke)
                SmokeBomb.Detonate(item.Amount);
        }

        /// <summary>Eşyayı alır; en az bir tane alındıysa true.</summary>
        public bool TryPickUp(ItemDefinition definition, int count, out int leftover)
        {
            leftover = Inventory.Add(definition.ToData(), count);
            return leftover < count;
        }
    }
}
