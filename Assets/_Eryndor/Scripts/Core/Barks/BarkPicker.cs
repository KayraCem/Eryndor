// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using System;
using System.Collections.Generic;
using Eryndor.Core.Util;

namespace Eryndor.Core.Barks
{
    /// <summary>Bir NPC'nin kısa sesli/yazılı tepki verdiği anlar.</summary>
    public enum BarkTrigger
    {
        BattleStart,
        PlayerSpotted,
        Countered,
        PlayerRepeatsCounter,
        LowHealth,
        PhaseChange,
        Death,
        PlayerFirstEntry
    }

    [Serializable]
    public struct BarkLine
    {
        public BarkTrigger trigger;
        public string text;

        public BarkLine(BarkTrigger trigger, string text)
        {
            this.trigger = trigger;
            this.text = text;
        }
    }

    /// <summary>
    /// Tetikleyiciye uyan replikler arasından seçer. Aynı repliği üst üste söylemez
    /// ve bekleme süresi dolmadan yeni bark vermez. LLM barkları eklenirse fallback buradan gelir.
    /// </summary>
    public sealed class BarkPicker
    {
        private readonly IReadOnlyList<BarkLine> _lines;
        private readonly IRandomSource _random;
        private readonly float _cooldown;
        private float _nextAllowedTime = float.NegativeInfinity;
        private string _lastText;

        public BarkPicker(IReadOnlyList<BarkLine> lines, IRandomSource random, float cooldownSeconds)
        {
            _lines = lines ?? Array.Empty<BarkLine>();
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _cooldown = Math.Max(0f, cooldownSeconds);
        }

        /// <summary>Uygun replik varsa döner; yoksa ya da bekleme sürüyorsa null.</summary>
        public string TryPick(BarkTrigger trigger, float now, bool ignoreCooldown = false)
        {
            if (!ignoreCooldown && now < _nextAllowedTime) return null;

            var options = new List<string>();
            foreach (var l in _lines)
                if (l.trigger == trigger && !string.IsNullOrWhiteSpace(l.text)) options.Add(l.text);
            if (options.Count == 0) return null;

            if (options.Count > 1) options.Remove(_lastText);
            string chosen = options[_random.NextInt(0, options.Count)];

            _lastText = chosen;
            _nextAllowedTime = now + _cooldown;
            return chosen;
        }
    }
}
