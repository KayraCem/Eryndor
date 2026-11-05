// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using System;
using System.Collections.Generic;
using Eryndor.Core.Stats;

namespace Eryndor.Core.Essence
{
    /// <summary>
    /// Oyuncunun taşıdığı Essence'lar (runtime durumu). Slotlar dolunca yeni Essence
    /// seçilen slottaki Essence'ın yerine geçer. Aynı Essence iki kez taşınamaz.
    /// </summary>
    public sealed class EssenceSlots
    {
        private readonly EssenceData[] _slots;

        public int Capacity => _slots.Length;
        public bool IsEmpowered { get; private set; }
        public float EmpowerMultiplier { get; private set; } = 1f;

        /// <summary>Slotlar değişince ya da Güçlendirme açılıp kapanınca tetiklenir.</summary>
        public event Action Changed;

        public EssenceSlots(int capacity)
        {
            if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            _slots = new EssenceData[capacity];
        }

        public EssenceData Get(int index) => _slots[index];

        public IEnumerable<EssenceData> All()
        {
            foreach (var e in _slots)
                if (e != null) yield return e;
        }

        public int Count
        {
            get { int n = 0; foreach (var e in _slots) if (e != null) n++; return n; }
        }

        public bool IsFull => Count == Capacity;

        public bool Contains(string essenceId)
        {
            foreach (var e in _slots)
                if (e != null && e.Id == essenceId) return true;
            return false;
        }

        /// <summary>
        /// Essence emer. Boş slot varsa oraya koyar. Slotlar doluysa replaceIndex gerekir.
        /// Yerinden çıkan Essence "replaced" ile döner (yoksa null).
        /// </summary>
        public bool TryAbsorb(EssenceData essence, int replaceIndex, out EssenceData replaced)
        {
            replaced = null;
            if (essence == null || Capacity == 0 || Contains(essence.Id)) return false;

            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] == null)
                {
                    _slots[i] = essence;
                    Changed?.Invoke();
                    return true;
                }
            }

            if (replaceIndex < 0 || replaceIndex >= _slots.Length) return false;
            replaced = _slots[replaceIndex];
            _slots[replaceIndex] = essence;
            Changed?.Invoke();
            return true;
        }

        public bool TryAbsorb(EssenceData essence) => TryAbsorb(essence, -1, out _);

        public void SetEmpowered(bool empowered, float multiplier)
        {
            IsEmpowered = empowered;
            EmpowerMultiplier = empowered ? Math.Max(1f, multiplier) : 1f;
            Changed?.Invoke();
        }

        /// <summary>Bir bonus türünün tüm slotlardaki toplamı, Güçlendirme çarpanı dahil.</summary>
        public float Total(EssenceBonusType type)
        {
            float sum = 0f;
            foreach (var e in _slots)
            {
                if (e == null) continue;
                foreach (var b in e.Bonuses)
                    if (b.type == type) sum += b.value;
            }
            return sum * EmpowerMultiplier;
        }

        /// <summary>Düz stat bonusları (FlatStrength vb.) bir StatBlock olarak.</summary>
        public StatBlock FlatStatBonus() => new StatBlock(
            (int)Math.Round(Total(EssenceBonusType.FlatStrength)),
            (int)Math.Round(Total(EssenceBonusType.FlatAgility)),
            (int)Math.Round(Total(EssenceBonusType.FlatVitality)));
    }
}
