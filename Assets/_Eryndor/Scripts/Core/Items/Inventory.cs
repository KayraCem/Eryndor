// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using System;

namespace Eryndor.Core.Items
{
    /// <summary>Envanterde bir slot: eşya + adet.</summary>
    public sealed class InventorySlot
    {
        public ItemData Item { get; internal set; }
        public int Count { get; internal set; }
        public bool IsEmpty => Item == null || Count <= 0;
    }

    /// <summary>
    /// Küçük, slot tabanlı tüketilebilir envanteri (GDD: 4 slot). Aynı eşyalar üst üste biner.
    /// </summary>
    public sealed class Inventory
    {
        private readonly InventorySlot[] _slots;

        public int Capacity => _slots.Length;
        public event Action Changed;

        public Inventory(int capacity)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            _slots = new InventorySlot[capacity];
            for (int i = 0; i < capacity; i++) _slots[i] = new InventorySlot();
        }

        public InventorySlot GetSlot(int index) => _slots[index];

        public int CountOf(string itemId)
        {
            int n = 0;
            foreach (var s in _slots)
                if (!s.IsEmpty && s.Item.Id == itemId) n += s.Count;
            return n;
        }

        /// <summary>Eşya ekler. Sığmayan adedi döner (0 = hepsi eklendi).</summary>
        public int Add(ItemData item, int count)
        {
            if (item == null || count <= 0) return count < 0 ? 0 : count;
            int remaining = count;

            // Önce aynı eşyanın yığınlarını doldur
            foreach (var s in _slots)
            {
                if (remaining == 0) break;
                if (s.IsEmpty || s.Item.Id != item.Id) continue;
                int space = item.MaxStack - s.Count;
                int moved = Math.Min(space, remaining);
                s.Count += moved;
                remaining -= moved;
            }

            // Sonra boş slotlar
            foreach (var s in _slots)
            {
                if (remaining == 0) break;
                if (!s.IsEmpty) continue;
                int moved = Math.Min(item.MaxStack, remaining);
                s.Item = item;
                s.Count = moved;
                remaining -= moved;
            }

            if (remaining != count) Changed?.Invoke();
            return remaining;
        }

        /// <summary>Slottaki eşyadan bir tane harcar ve onu döner (boşsa null).</summary>
        public ItemData ConsumeOne(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slots.Length) return null;
            var s = _slots[slotIndex];
            if (s.IsEmpty) return null;

            var item = s.Item;
            s.Count--;
            if (s.Count <= 0) { s.Item = null; s.Count = 0; }
            Changed?.Invoke();
            return item;
        }
    }
}
