// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
namespace Eryndor.Core.Combat
{
    /// <summary>Bir koşunun özeti; zafer/ölüm ekranında gösterilir (GDD 9).</summary>
    public sealed class RunStats
    {
        public float ElapsedSeconds { get; private set; }
        public int Kills { get; private set; }
        public int SuccessfulCounters { get; private set; }
        public int EssencesAbsorbed { get; private set; }

        public void Tick(float deltaTime) { if (deltaTime > 0f) ElapsedSeconds += deltaTime; }
        public void AddKill() => Kills++;
        public void AddCounter() => SuccessfulCounters++;
        public void AddEssence() => EssencesAbsorbed++;

        public string FormatTime()
        {
            int total = (int)ElapsedSeconds;
            return $"{total / 60:00}:{total % 60:00}";
        }
    }
}
