// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using System;
using System.Collections.Generic;
using Eryndor.Core.Combat;
using Eryndor.Core.Util;

namespace Eryndor.Core.AI
{
    /// <summary>
    /// Boss'un saldırı seçimi (GDD 5.2, normal saldırı dalı):
    ///  - Sadece menzile uyan saldırılar aday olur.
    ///  - Aynı saldırı üst üste en fazla maxRepeats kez seçilir.
    ///  - Ağırlıklı rastgele seçim; uyum dalı counter'lanamaz saldırıların ağırlığını artırabilir.
    /// </summary>
    public sealed class AttackSelector
    {
        private readonly IRandomSource _random;
        private readonly int _maxRepeats;
        private string _lastId;
        private int _repeatCount;

        /// <summary>Counter'lanamaz saldırıların ağırlık çarpanı (uyum dalı değiştirir).</summary>
        public float UncounterableWeightMultiplier { get; set; } = 1f;

        public AttackSelector(IRandomSource random, int maxRepeats = 2)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _maxRepeats = Math.Max(1, maxRepeats);
        }

        /// <summary>Uygun saldırı yoksa null döner (boss yaklaşmalı).</summary>
        public AttackData Pick(IReadOnlyList<AttackData> attacks, float distance)
        {
            if (attacks == null) return null;
            var candidates = new List<AttackData>();
            var weights = new List<float>();
            float total = 0f;

            foreach (var a in attacks)
            {
                if (a == null || distance < a.minRange || distance > a.maxRange) continue;
                if (a.id == _lastId && _repeatCount >= _maxRepeats) continue;
                float w = a.weight * (a.counterable ? 1f : UncounterableWeightMultiplier);
                if (w <= 0f) continue;
                candidates.Add(a);
                weights.Add(w);
                total += w;
            }

            if (candidates.Count == 0) return null;

            float pick = _random.NextFloat() * total;
            AttackData chosen = candidates[candidates.Count - 1];
            for (int i = 0; i < candidates.Count; i++)
            {
                if (pick < weights[i]) { chosen = candidates[i]; break; }
                pick -= weights[i];
            }

            if (chosen.id == _lastId) _repeatCount++;
            else { _lastId = chosen.id; _repeatCount = 1; }
            return chosen;
        }
    }
}
