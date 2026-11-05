// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using System;
using System.Collections.Generic;

namespace Eryndor.Core.AI
{
    public enum PlayerActionType { LightAttack, HeavyAttack, Counter, Dodge, Technique }

    /// <summary>
    /// Boss'un "uyum dalı" için oyuncunun son N hamlesini sayar (GDD 5.2).
    /// Örnek: son 10 hamlenin %60'ından fazlası Counter ise boss sahte saldırıları artırır.
    /// </summary>
    public sealed class PlayerHabitTracker
    {
        private readonly Queue<PlayerActionType> _history = new Queue<PlayerActionType>();
        private readonly int _capacity;

        public int Count => _history.Count;
        public int Capacity => _capacity;

        public PlayerHabitTracker(int capacity = 10)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            _capacity = capacity;
        }

        public void Record(PlayerActionType action)
        {
            _history.Enqueue(action);
            while (_history.Count > _capacity) _history.Dequeue();
        }

        /// <summary>Bir hamle türünün son hamleler içindeki payı (0..1). Veri yoksa 0.</summary>
        public float Share(PlayerActionType action)
        {
            if (_history.Count == 0) return 0f;
            int n = 0;
            foreach (var a in _history) if (a == action) n++;
            return (float)n / _history.Count;
        }

        /// <summary>Yeterli veri varsa ve pay eşiği geçiyorsa true.</summary>
        public bool IsRelyingOn(PlayerActionType action, float threshold, int minSamples = 5) =>
            _history.Count >= minSamples && Share(action) > threshold;

        public void Clear() => _history.Clear();
    }
}
