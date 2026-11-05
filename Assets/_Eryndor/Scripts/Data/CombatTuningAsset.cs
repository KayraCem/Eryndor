// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using Eryndor.Core.Combat;
using Eryndor.Core.Validation;
using UnityEngine;

namespace Eryndor.Data
{
    /// <summary>Tüm dengeleme değerlerinin Inspector'dan ayarlandığı tek asset.</summary>
    [CreateAssetMenu(menuName = "Eryndor/Combat Tuning", fileName = "CombatTuning")]
    public sealed class CombatTuningAsset : ScriptableObject, IValidatable
    {
        [SerializeField] private CombatConfig config = new CombatConfig();

        public CombatConfig Config => config;

        public ValidationResult Validate() => config.Validate();

        private void OnValidate()
        {
            var r = Validate();
            if (!r.IsValid || r.Warnings.Count > 0) Debug.LogWarning($"[CombatTuning] {name}\n{r}", this);
        }
    }
}
