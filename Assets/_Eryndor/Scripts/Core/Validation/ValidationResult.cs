// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using System.Collections.Generic;
using System.Text;

namespace Eryndor.Core.Validation
{
    /// <summary>
    /// Bir veri tanımının doğrulama sonucu. Hatalar oyunu bozar, uyarılar sadece dikkat ister.
    /// </summary>
    public sealed class ValidationResult
    {
        private readonly List<string> _errors = new List<string>();
        private readonly List<string> _warnings = new List<string>();

        public IReadOnlyList<string> Errors => _errors;
        public IReadOnlyList<string> Warnings => _warnings;
        public bool IsValid => _errors.Count == 0;

        public ValidationResult Error(string message) { _errors.Add(message); return this; }
        public ValidationResult Warning(string message) { _warnings.Add(message); return this; }

        /// <summary>Koşul yanlışsa hata ekler.</summary>
        public ValidationResult Require(bool condition, string message)
        {
            if (!condition) _errors.Add(message);
            return this;
        }

        /// <summary>Koşul yanlışsa uyarı ekler.</summary>
        public ValidationResult Recommend(bool condition, string message)
        {
            if (!condition) _warnings.Add(message);
            return this;
        }

        public ValidationResult Merge(ValidationResult other, string prefix = "")
        {
            if (other == null) return this;
            foreach (var e in other._errors) _errors.Add(prefix + e);
            foreach (var w in other._warnings) _warnings.Add(prefix + w);
            return this;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach (var e in _errors) sb.Append("HATA: ").AppendLine(e);
            foreach (var w in _warnings) sb.Append("UYARI: ").AppendLine(w);
            return sb.ToString();
        }
    }

    /// <summary>Kendini doğrulayabilen her veri tanımı bunu uygular.</summary>
    public interface IValidatable
    {
        ValidationResult Validate();
    }
}
