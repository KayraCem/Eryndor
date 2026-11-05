// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Eryndor.Runtime.UI
{
    /// <summary>
    /// Bir menü (ör. Essence değiştirme) açıkken oyuncu girdisini kapatır.
    /// </summary>
    public static class UIModal
    {
        public static bool IsOpen { get; private set; }

        public static void Open() => IsOpen = true;
        public static void Close() => IsOpen = false;

        // "Reload Domain" kapalıyken statik değerler Play'ler arasında kalır; her Play'de sıfırla.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => IsOpen = false;
    }

    /// <summary>
    /// Ekranın altındaki etkileşim ipucu ("F: Dinlen" gibi). Birden çok kaynak olabilir; biri gösterilir.
    /// </summary>
    public static class InteractionPrompt
    {
        private static readonly Dictionary<object, string> Active = new Dictionary<object, string>();

        public static event Action<string> Changed;

        public static string Current
        {
            get { foreach (var text in Active.Values) return text; return string.Empty; }
        }

        public static void Set(object owner, string text)
        {
            if (Active.TryGetValue(owner, out var current) && current == text) return;
            Active[owner] = text;
            Changed?.Invoke(Current);
        }

        public static void Clear(object owner)
        {
            if (Active.Remove(owner)) Changed?.Invoke(Current);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Active.Clear();
            Changed = null;
        }
    }
}
