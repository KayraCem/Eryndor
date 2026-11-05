// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using System;
using Eryndor.Core.Validation;

namespace Eryndor.Core.Combat
{
    /// <summary>
    /// Tek bir saldırının statik tanımı (oyuncu ya da düşman). Oyun sırasında değişmez.
    /// </summary>
    [Serializable]
    public sealed class AttackData : IValidatable
    {
        public string id = "attack";
        public string displayName = "Saldırı";
        public float baseDamage = 10f;
        [UnityLikeTooltip("Uyarı (telegraph) süresi: saldırı isabet etmeden önce geçen süre, sn.")]
        public float windupTime = 0.6f;
        public float recoveryTime = 0.5f;
        public float minRange = 0f;
        public float maxRange = 2f;
        [UnityLikeTooltip("false ise kırmızı uyarı: counter'lanamaz, kaçılmalı.")]
        public bool counterable = true;
        public bool isArea;
        [UnityLikeTooltip("Seçim ağırlığı (boss için). 0 ise hiç seçilmez.")]
        public float weight = 1f;

        public ValidationResult Validate()
        {
            var r = new ValidationResult();
            r.Require(!string.IsNullOrWhiteSpace(id), "Saldırının id'si boş.");
            r.Require(baseDamage >= 0f, $"{id}: hasar negatif olamaz.");
            r.Require(windupTime >= 0.1f, $"{id}: uyarı süresi 0,1 sn'den kısa; oyuncu tepki veremez.");
            r.Require(maxRange > minRange, $"{id}: maxRange, minRange'den büyük olmalı.");
            r.Require(weight >= 0f, $"{id}: ağırlık negatif olamaz.");
            return r;
        }
    }

    /// <summary>
    /// Core, UnityEngine'e bağlı olmadığı için [Tooltip] kullanamaz. Bu sadece okuyucuya not düşer.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class UnityLikeTooltipAttribute : Attribute
    {
        public string Text { get; }
        public UnityLikeTooltipAttribute(string text) { Text = text; }
    }
}
