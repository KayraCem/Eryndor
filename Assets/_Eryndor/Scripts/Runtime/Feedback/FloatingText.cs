// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using TMPro;
using UnityEngine;

namespace Eryndor.Runtime.Feedback
{
    /// <summary>Yukarı süzülüp kaybolan hasar sayısı.</summary>
    public sealed class FloatingText : MonoBehaviour
    {
        private TextMeshPro _text;
        private float _age;
        private float _lifetime = 0.8f;
        private Color _color;
        private Camera _camera;

        public static void Spawn(Vector3 position, string text, Color color, float size)
        {
            var go = new GameObject("HasarSayisi");
            go.transform.position = position + new Vector3(Random.Range(-0.3f, 0.3f), 0f, 0f);
            var t = go.AddComponent<TextMeshPro>();
            t.text = text;
            t.fontSize = size;
            t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center;
            t.color = color;
            t.rectTransform.sizeDelta = new Vector2(3f, 1f);
            var f = go.AddComponent<FloatingText>();
            f._text = t;
            f._color = color;
        }

        private void Awake() => _camera = Camera.main;

        private void Update()
        {
            _age += Time.unscaledDeltaTime;
            transform.position += Vector3.up * (1.4f * Time.unscaledDeltaTime);
            if (_camera != null) transform.rotation = _camera.transform.rotation;
            float k = Mathf.Clamp01(_age / _lifetime);
            _text.color = new Color(_color.r, _color.g, _color.b, 1f - k * k);
            if (_age >= _lifetime) Destroy(gameObject);
        }
    }
}
