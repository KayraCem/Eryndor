// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
namespace Eryndor.Core.Combat
{
    public enum CounterResult
    {
        None,       // Duruş yoktu ya da saldırı counter'lanamazdı: normal isabet
        Success     // Saldırı boşa çıktı, oyuncu karşı vuruş yapar
    }

    /// <summary>
    /// Counter mekaniğinin zamanlama mantığı. Unity'den bağımsızdır; zaman dışarıdan verilir.
    ///
    /// Oyuncu counter'a basınca "pencere" kadar süren bir duruş açılır.
    ///  - Bu sürede counter'lanabilir bir saldırı gelirse: Success, ceza yok.
    ///  - Duruş boşa biterse: "ceza" kadar süre savunmasız kalır ve yeniden counter yapamaz.
    /// Böylece counter tuşunu sürekli basmak işe yaramaz.
    /// </summary>
    public sealed class CounterStance
    {
        private float _stanceEndsAt = float.NegativeInfinity;
        private float _whiffPenalty;
        private bool _consumed = true;

        /// <summary>Duruş açık ve henüz kullanılmadı mı?</summary>
        public bool IsActive(float now) => !_consumed && now < _stanceEndsAt;

        /// <summary>Duruş boşa bitti ve ceza süresi devam ediyor mu?</summary>
        public bool IsVulnerable(float now) =>
            !_consumed && now >= _stanceEndsAt && now < _stanceEndsAt + _whiffPenalty;

        public bool CanBegin(float now) => !IsActive(now) && !IsVulnerable(now);

        /// <summary>Counter duruşunu başlatır. Duruş ya da ceza sürüyorsa false döner.</summary>
        public bool TryBegin(float now, float window, float whiffPenalty)
        {
            if (!CanBegin(now)) return false;
            _stanceEndsAt = now + window;
            _whiffPenalty = whiffPenalty;
            _consumed = false;
            return true;
        }

        /// <summary>Gelen bir saldırı isabet anında buraya sorulur.</summary>
        public CounterResult ResolveIncoming(float now, bool attackIsCounterable)
        {
            if (attackIsCounterable && IsActive(now))
            {
                _consumed = true; // başarılı counter cezayı iptal eder
                return CounterResult.Success;
            }
            return CounterResult.None;
        }
    }
}
