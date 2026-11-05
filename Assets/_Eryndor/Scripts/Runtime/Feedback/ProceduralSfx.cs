// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using UnityEngine;

namespace Eryndor.Runtime.Feedback
{
    /// <summary>
    /// Ses dosyası olmadan, kodla basit efekt sesleri üretir (yer tutucu).
    /// Gerçek sesler (Freesound, CC0) gelince CombatFeedback'teki AudioClip alanlarına atanır.
    /// </summary>
    public static class ProceduralSfx
    {
        private const int Rate = 44100;

        /// <summary>Counter "çın" sesi: metalik, uzun çınlama.</summary>
        public static AudioClip CounterRing() => Make("Sfx_Counter", 0.6f, (t, n) =>
            (Mathf.Sin(2f * Mathf.PI * 1320f * t) + 0.6f * Mathf.Sin(2f * Mathf.PI * 2650f * t) + 0.3f * n * Mathf.Exp(-60f * t))
            * Mathf.Exp(-6f * t));

        /// <summary>Yumruk isabeti: kısa, boğuk darbe.</summary>
        public static AudioClip Punch() => Make("Sfx_Punch", 0.14f, (t, n) =>
            (0.7f * n + Mathf.Sin(2f * Mathf.PI * 95f * t)) * Mathf.Exp(-35f * t));

        /// <summary>Oyuncu hasar aldı.</summary>
        public static AudioClip Hurt() => Make("Sfx_Hurt", 0.22f, (t, n) =>
            (Mathf.Sin(2f * Mathf.PI * (240f - 300f * t) * t) + 0.3f * n) * Mathf.Exp(-14f * t));

        /// <summary>Saldırı uyarısı: counter'lanabilir (tiz) ya da kaç (pes, iki vuruşlu).</summary>
        public static AudioClip WarningCounterable() => Make("Sfx_WarnYellow", 0.12f, (t, n) =>
            Mathf.Sin(2f * Mathf.PI * 880f * t) * Mathf.Exp(-18f * t));

        public static AudioClip WarningDanger() => Make("Sfx_WarnRed", 0.3f, (t, n) =>
            Mathf.Sin(2f * Mathf.PI * 220f * t) * (t < 0.12f ? 1f : (t > 0.16f ? 1f : 0f)) * Mathf.Exp(-6f * t));

        private static AudioClip Make(string name, float duration, System.Func<float, float, float> wave)
        {
            int samples = Mathf.CeilToInt(Rate * duration);
            var data = new float[samples];
            var rng = new System.Random(name.GetHashCode());
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / Rate;
                float noise = (float)(rng.NextDouble() * 2.0 - 1.0);
                data[i] = Mathf.Clamp(wave(t, noise) * 0.45f, -1f, 1f);
            }
            var clip = AudioClip.Create(name, samples, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
