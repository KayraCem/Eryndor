// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using Eryndor.Core.Validation;
using UnityEngine;

namespace Eryndor.Data
{
    /// <summary>
    /// Tüm statik tanımların temeli. Her tanımın benzersiz bir id'si vardır ve kendini doğrular.
    /// Inspector'da değer değişince OnValidate hataları Console'a yazar.
    /// </summary>
    public abstract class DefinitionBase : ScriptableObject, IValidatable
    {
        [SerializeField] protected string id;
        [SerializeField] protected string displayName;
        [SerializeField, TextArea] protected string description;

        public string Id => id;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public string Description => description;

        public ValidationResult Validate()
        {
            var r = new ValidationResult();
            r.Require(!string.IsNullOrWhiteSpace(id), "id boş.");
            r.Recommend(!string.IsNullOrWhiteSpace(displayName), "displayName boş; asset adı kullanılacak.");
            ValidateFields(r);
            return r;
        }

        /// <summary>Alt sınıflar kendi alan kontrollerini buraya ekler.</summary>
        protected abstract void ValidateFields(ValidationResult result);

        protected virtual void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(id)) id = name; // yeni asset'te id'yi dosya adından doldur
            var r = Validate();
            if (!r.IsValid) Debug.LogWarning($"[{GetType().Name}] {name}\n{r}", this);
        }

#if UNITY_EDITOR
        /// <summary>Örnek veri üreticisi (Editor) için alan atama yardımcısı.</summary>
        public void EditorSetIdentity(string newId, string newDisplayName, string newDescription)
        {
            id = newId;
            displayName = newDisplayName;
            description = newDescription;
        }
#endif
    }
}
