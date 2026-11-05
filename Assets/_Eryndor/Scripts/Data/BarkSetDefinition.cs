// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using System.Collections.Generic;
using Eryndor.Core.Barks;
using Eryndor.Core.Validation;
using UnityEngine;

namespace Eryndor.Data
{
    /// <summary>Bir karakterin bark replikleri (GDD 7, diyalog listesi).</summary>
    [CreateAssetMenu(menuName = "Eryndor/Bark Set", fileName = "Barks_")]
    public sealed class BarkSetDefinition : DefinitionBase
    {
        [SerializeField, Min(0f)] private float cooldownSeconds = 4f;
        [SerializeField] private List<BarkLine> lines = new List<BarkLine>();

        public float CooldownSeconds => cooldownSeconds;
        public IReadOnlyList<BarkLine> Lines => lines;

        protected override void ValidateFields(ValidationResult r)
        {
            r.Require(lines.Count > 0, "Hiç replik yok.");
            foreach (var l in lines)
            {
                r.Require(!string.IsNullOrWhiteSpace(l.text), $"{l.trigger}: boş replik.");
                r.Recommend(l.text == null || l.text.Length <= 60, $"{l.trigger}: replik 60 karakterden uzun, ekranda okunmayabilir.");
            }
        }

#if UNITY_EDITOR
        public void EditorSet(float cooldown, params BarkLine[] newLines)
        {
            cooldownSeconds = cooldown; lines = new List<BarkLine>(newLines);
        }
#endif
    }
}
