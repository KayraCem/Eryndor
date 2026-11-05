// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
namespace Eryndor.Core.Items
{
    /// <summary>Nadirlik seviyeleri (GDD 4.5).</summary>
    public enum Rarity
    {
        Common = 0,     // Yaygın
        Rare = 1,       // Nadir
        Legendary = 2   // Efsanevi
    }

    public static class RarityExtensions
    {
        public static string ToTurkish(this Rarity rarity)
        {
            switch (rarity)
            {
                case Rarity.Rare: return "Nadir";
                case Rarity.Legendary: return "Efsanevi";
                default: return "Yaygın";
            }
        }
    }
}
