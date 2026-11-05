# Eryndor: İlk Kat — Unity proje iskeleti

Dark fantasy, üçüncü şahıs, tek katlı zindan aksiyon RPG'si. Silahsız bir Dövüş Sanatçısı (Kael), counter mekaniği ve Essence sistemi. Game Studio: AI in Game Development dersi, bireysel proje.

Tüm script'ler **[AI destekli]** başlığı taşır (ders kuralı). Her dosyayı okuyup anladıktan sonra başlıktaki `İnceleyen: Kayra — [tarih]` kısmını doldur.

## Mimari

| Katman | Klasör | Assembly | Görevi |
| --- | --- | --- | --- |
| Çekirdek | `Scripts/Core` | `Eryndor.Core` (UnityEngine yok) | Formüller, counter zamanlaması, CharacterState, Essence, envanter, loot, FSM, behavior tree, bark seçimi. Saf C#, birim testli. |
| Veri | `Scripts/Data` | `Eryndor.Data` | ScriptableObject tanımları (statik veri): CombatTuning, Character, Enemy, Ability, Essence, Item, LootTable, BarkSet. Her biri `Validate()` ile kendini doğrular. |
| Oyun | `Scripts/Runtime` | `Eryndor.Runtime` | MonoBehaviour'lar: oyuncu, düşman state machine'i, boss behavior tree'si, pickup'lar, HUD, GameManager. |
| Editor | `Scripts/Editor` | `Eryndor.Editor` | Menüler: örnek veri üretme ve tüm veriyi doğrulama. |
| Testler | `Tests/EditMode` | `Eryndor.Tests.EditMode` | Çekirdek için NUnit testleri. |

Temel kural: **statik tanımlar oyun sırasında değişmez.** Can, Mana, Stamina, Essence slotları gibi değişen her şey `CharacterState` gibi runtime sınıflarında tutulur.

## Kurulum

1. Unity Hub'da **Unity 6 (6000.x)**, **Universal 3D (URP)** şablonuyla yeni proje aç.
2. Package Manager'dan kur: **Input System**, **AI Navigation**, **Cinemachine**. (Test Framework ve TextMeshPro zaten gelir.)
3. Input System kurulunca Unity yeniden başlatmayı sorar: **Yes**. `Project Settings > Player > Active Input Handling` = **Input System Package** ya da **Both**.
4. Bu repodaki `Assets/_Eryndor` klasörünü projenin `Assets` klasörüne kopyala.
5. Derlemenin bitmesini bekle; Console'da kırmızı hata kalmamalı. Hata varsa mesajın tamamını kopyala.
6. Menü: **Eryndor > Örnek Verileri Oluştur**. `Assets/_Eryndor/Data` altına GDD'deki örnek canavarlar, Essence'lar, teknikler, eşyalar ve barklar gelir.
7. Menü: **Window > General > Test Runner > EditMode > Run All**. Tüm testler yeşil olmalı.

## Sahneleri kurma

1. **Window > TextMeshPro > Import TMP Essential Resources** (bir kez).
2. Menü: **Eryndor > Test Sahnesini Kur**. İki sahne oluşur ve Build Settings'e eklenir:
   - `MainMenu.unity`: Başla / Çıkış
   - `TestArena.unity`: GDD'deki İlk Kat. **Taş Koridor** (2 Kemik Askeri, 1 Mağara Kurdu) → **Dinlenme Kampı** (sunak, F ile can dolar) → **Bekçi Salonu** (Gorrath; girince kapı kapanır).
3. Kurulumun sonunda araç bağlantıları kendisi kontrol eder; sorun varsa Console'a kırmızı yazar.
4. **Play**. Fare kamerayı döndürür; Esc duraklatır.

Dövüş geri bildirimi `CombatFeedback` içinde: hit-stop, counter yavaş çekimi, kamera sarsıntısı, hasar sayıları, vuruş parlaması ve kodla üretilen yer tutucu sesler (saldırı uyarısı: tiz = counter'la, pes = kaç). Gerçek ses dosyaları gelince Inspector'daki alanlara atanır.

Grafikler yer tutucudur (kapsül ve küp). Modeller hazır olunca `Govde` objelerinin yerine konur.

## Kontroller

| Eylem | Klavye + fare | Gamepad |
| --- | --- | --- |
| Hareket | WASD | Sol analog |
| Hafif vuruş (3'lü kombo) | Sol tık | X / Kare |
| Ağır vuruş | Sağ tık | Y / Üçgen |
| Counter | Space | RB / R1 |
| Kaçış | Shift | B / Daire |
| Teknik 1 / 2 | Q / E | LB + X / LB + Y |
| Essence Güçlendirme | R (basılı) | LT |
| Essence em | F | A / Çarpı |
| Eşya kullan | 1-4 | — |
| Duraklat | Esc | Start |

## Dengeleme

Bütün sayılar `Data/CombatTuning.asset` ve tanım asset'lerinde. Değiştirdikten sonra **Eryndor > Verileri Doğrula** çalıştır; hatalı ya da riskli değerler Console'a yazılır.

## Bilinen eksikler / sıradakiler

- Animasyonlar: `Animator` alanı boş bırakılabilir; trigger adları script açıklamalarında.
- Gerçek modeller, ses dosyaları ve VFX.
- LLM barkları (nice-to-have): `BarkPicker` fallback olarak hazır.
