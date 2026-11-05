// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using Eryndor.Core.Validation;
using UnityEditor;
using UnityEngine;

namespace Eryndor.EditorTools
{
    /// <summary>
    /// Menü: Eryndor > Verileri Doğrula. _Eryndor klasöründeki tüm tanımları kontrol eder
    /// ve hataları Console'a yazar. Teslimden önce ve her dengeleme değişikliğinden sonra çalıştır.
    /// </summary>
    public static class DataValidatorMenu
    {
        private const string Root = "Assets/_Eryndor";

        [MenuItem("Eryndor/Verileri Doğrula")]
        public static void ValidateAll()
        {
            int checkedCount = 0, errorCount = 0, warningCount = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { Root }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (!(asset is IValidatable validatable)) continue;

                checkedCount++;
                var result = validatable.Validate();
                errorCount += result.Errors.Count;
                warningCount += result.Warnings.Count;

                if (!result.IsValid) Debug.LogError($"[Doğrulama] {path}\n{result}", asset);
                else if (result.Warnings.Count > 0) Debug.LogWarning($"[Doğrulama] {path}\n{result}", asset);
            }

            string summary = $"[Doğrulama] {checkedCount} tanım kontrol edildi: {errorCount} hata, {warningCount} uyarı.";
            if (errorCount > 0) Debug.LogError(summary); else Debug.Log(summary);
        }
    }
}
