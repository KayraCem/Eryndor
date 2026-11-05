// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using System;
using UnityEngine;

namespace Eryndor.Runtime.Combat
{
    /// <summary>
    /// Bark'ların HUD'a ulaştığı basit olay kanalı. Konuşan kim olursa olsun HUD tek yerden dinler.
    /// </summary>
    public static class BarkBus
    {
        /// <summary>(konuşan adı, replik)</summary>
        public static event Action<string, string> Spoken;

        public static void Say(string speaker, string line)
        {
            if (!string.IsNullOrEmpty(line)) Spoken?.Invoke(speaker, line);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Spoken = null;
    }
}
