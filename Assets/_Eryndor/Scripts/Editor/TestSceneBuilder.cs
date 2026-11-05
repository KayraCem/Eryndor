// Eryndor: İlk Kat
// [AI destekli] Claude (Anthropic) ile taslak oluşturuldu. İnceleyen: Kayra — [tarih]
using System.Collections.Generic;
using System.IO;
using Eryndor.Data;
using Eryndor.Runtime.Boss;
using Eryndor.Runtime.Combat;
using Eryndor.Runtime.Enemies;
using Eryndor.Runtime.Feedback;
using Eryndor.Runtime.Game;
using Eryndor.Runtime.Pickups;
using Eryndor.Runtime.Player;
using Eryndor.Runtime.UI;
using Eryndor.Runtime.World;
using TMPro;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Eryndor.EditorTools
{
    /// <summary>
    /// Menü: Eryndor > Test Sahnesini Kur. GDD'deki 3 odalı İlk Kat'ı kurar:
    /// Taş Koridor (2 Kemik Askeri + 1 Mağara Kurdu) → Dinlenme Kampı (sunak) → Bekçi Salonu (Gorrath).
    /// Ayrıca NavMesh, üçüncü şahıs kamera, dövüş geri bildirimi, HUD, Essence değiştirme paneli,
    /// zafer/ölüm ve duraklatma ekranları ile ana menü sahnesini kurar.
    /// Grafikler yer tutucudur (kapsül ve küp); asıl modeller sonra değiştirilir.
    /// </summary>
    public static class TestSceneBuilder
    {
        private const string SceneDir = "Assets/_Eryndor/Scenes";
        private const string ScenePath = SceneDir + "/TestArena.unity";
        private const string MenuScenePath = SceneDir + "/MainMenu.unity";
        private const string NavMeshPath = SceneDir + "/TestArena_NavMesh.asset";
        private const string MaterialDir = "Assets/_Eryndor/Art/Materials";
        private const string PrefabDir = "Assets/_Eryndor/Prefabs";
        private const string DataDir = "Assets/_Eryndor/Data";

        [MenuItem("Eryndor/Test Sahnesini Kur")]
        public static void Build()
        {
            bool tmpReady;
            try { tmpReady = TMP_Settings.instance != null && TMP_Settings.defaultFontAsset != null; }
            catch { tmpReady = false; }
            if (!tmpReady)
            {
                EditorUtility.DisplayDialog("TextMeshPro eksik",
                    "Önce Window > TextMeshPro > Import TMP Essential Resources ile TMP dosyalarını içe aktar, sonra bu menüyü tekrar çalıştır.",
                    "Tamam");
                return;
            }
            if (File.Exists(ScenePath) && !EditorUtility.DisplayDialog("Sahne zaten var",
                    "TestArena sahnesi baştan kurulsun mu? Sahnede elle yaptığın değişiklikler silinir.", "Evet, baştan kur", "Vazgeç"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            SampleDataFactory.CreateAll();
            Directory.CreateDirectory(SceneDir);
            Directory.CreateDirectory(MaterialDir);
            Directory.CreateDirectory(PrefabDir);
            AssetDatabase.Refresh();

            int wallsLayer = EnsureLayer("Walls");
            if (wallsLayer < 0) { Debug.LogError("[Eryndor] 'Walls' katmanı eklenemedi: boş katman yok."); return; }
            int wallsMask = 1 << wallsLayer;

            // Not: Yeni sahne açmak, önceden yüklenmiş asset referanslarını boşa düşürebiliyor.
            // Bu yüzden veriler yeni sahne açıldıktan SONRA yüklenir.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            SetupAtmosphere();

            var tuning = Load<CombatTuningAsset>("CombatTuning");
            var kael = Load<CharacterDefinition>("Character_Kael");
            var bone = Load<EnemyDefinition>("Enemy_KemikAskeri");
            var wolf = Load<EnemyDefinition>("Enemy_MagaraKurdu");
            var gorrath = Load<EnemyDefinition>("Enemy_Gorrath");
            var potion = Load<ItemDefinition>("Item_SifaIksiri");
            var smoke = Load<ItemDefinition>("Item_DumanBombasi");
            if (tuning == null || kael == null || bone == null || wolf == null || gorrath == null || potion == null || smoke == null)
            {
                Debug.LogError("[Eryndor] Örnek veriler bulunamadı. Önce Eryndor > Örnek Verileri Oluştur'u çalıştır.");
                return;
            }

            // ---- Malzemeler ve prefab'lar ----
            var matGround = Mat("M_Zemin", new Color(0.16f, 0.15f, 0.14f));
            var matWall = Mat("M_Duvar", new Color(0.30f, 0.28f, 0.26f));
            var matKael = Mat("M_Kael", new Color(0.85f, 0.72f, 0.50f));
            var matBone = Mat("M_KemikAskeri", new Color(0.82f, 0.80f, 0.70f));
            var matWolf = Mat("M_MagaraKurdu", new Color(0.36f, 0.34f, 0.42f));
            var matBoss = Mat("M_Gorrath", new Color(0.45f, 0.12f, 0.10f));
            var matEssence = Mat("M_Essence", new Color(0.45f, 0.75f, 1f), true);
            var matItem = Mat("M_Esya", new Color(0.30f, 0.85f, 0.40f));

            var essencePrefab = CreateEssencePrefab(matEssence);
            var itemPrefab = CreateItemPrefab(matItem);

            // ---- Seviye: 40 × 60 m, ortada kapılı bir ara duvar ----
            var level = new GameObject("Level");
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Zemin";
            ground.transform.SetParent(level.transform);
            ground.transform.localScale = new Vector3(4f, 1f, 6f);
            SetMaterial(ground, matGround);

            Wall(level.transform, "Duvar_Sol", new Vector3(-20.5f, 1.5f, 0f), new Vector3(1f, 3f, 62f), matWall, wallsLayer);
            Wall(level.transform, "Duvar_Sag", new Vector3(20.5f, 1.5f, 0f), new Vector3(1f, 3f, 62f), matWall, wallsLayer);
            Wall(level.transform, "Duvar_Arka", new Vector3(0f, 1.5f, -30.5f), new Vector3(42f, 3f, 1f), matWall, wallsLayer);
            Wall(level.transform, "Duvar_On", new Vector3(0f, 1.5f, 30.5f), new Vector3(42f, 3f, 1f), matWall, wallsLayer);
            Wall(level.transform, "AraDuvar_Sol", new Vector3(-11f, 1.5f, 2.5f), new Vector3(18f, 3f, 1f), matWall, wallsLayer);
            Wall(level.transform, "AraDuvar_Sag", new Vector3(11f, 1.5f, 2.5f), new Vector3(18f, 3f, 1f), matWall, wallsLayer);
            // Taş Koridor ile Dinlenme Kampı arası (z = -8), ortada 4 m kapı
            Wall(level.transform, "KoridorDuvari_Sol", new Vector3(-11f, 1.5f, -8f), new Vector3(18f, 3f, 1f), matWall, wallsLayer);
            Wall(level.transform, "KoridorDuvari_Sag", new Vector3(11f, 1.5f, -8f), new Vector3(18f, 3f, 1f), matWall, wallsLayer);
            Wall(level.transform, "Sutun_1", new Vector3(-4f, 1.5f, -15f), new Vector3(2f, 3f, 2f), matWall, wallsLayer);
            Wall(level.transform, "Sutun_2", new Vector3(5f, 1.5f, -24f), new Vector3(2f, 3f, 2f), matWall, wallsLayer);
            Wall(level.transform, "Sutun_Salon_1", new Vector3(-8f, 1.5f, 16f), new Vector3(2f, 3f, 2f), matWall, wallsLayer);
            Wall(level.transform, "Sutun_Salon_2", new Vector3(8f, 1.5f, 16f), new Vector3(2f, 3f, 2f), matWall, wallsLayer);

            var surface = level.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.BuildNavMesh();

            // Kapı engeli Level'in dışında: NavMesh'e girmez, boss savaşı başlayınca açılır
            var blocker = Wall(null, "Kapi_Engeli", new Vector3(0f, 1.5f, 2.5f), new Vector3(4f, 3f, 1f), matWall, wallsLayer);
            blocker.SetActive(false);

            // ---- Oyuncu ----
            var player = new GameObject("Kael") { tag = "Player" };
            player.transform.position = new Vector3(0f, 0.05f, -27f);
            var cc = player.AddComponent<CharacterController>();
            cc.center = new Vector3(0f, 1f, 0f);
            cc.height = 2f;
            cc.radius = 0.4f;
            Body(player.transform, matKael);
            var playerAim = Child(player.transform, "AimPoint", new Vector3(0f, 1.2f, 0f));
            var playerCombatant = player.AddComponent<Combatant>();
            SetEnum(playerCombatant, "team", (int)Team.Player);
            SetRef(playerCombatant, "aimPoint", playerAim);
            player.AddComponent<PlayerMotor>();
            player.AddComponent<PlayerInputReader>();
            var playerCombat = player.AddComponent<PlayerCombat>();
            SetRef(playerCombat, "definition", kael);
            SetRef(playerCombat, "tuning", tuning);
            var playerInventory = player.AddComponent<PlayerInventory>();
            SetStartingItems(playerInventory, (potion, 2), (smoke, 1));

            // ---- Kamera ----
            var cameraGo = Camera.main != null ? Camera.main.gameObject : new GameObject("Main Camera", typeof(Camera)) { tag = "MainCamera" };
            cameraGo.transform.position = player.transform.position + new Vector3(0f, 3.5f, -6f);
            var tpc = cameraGo.AddComponent<ThirdPersonCamera>();
            SetRef(tpc, "target", player.transform);
            SetMask(tpc, "collisionMask", wallsMask);

            // ---- Düşmanlar (başlangıç odası) ----
            var points = new GameObject("DevriyeNoktalari").transform;
            Enemy("KemikAskeri_A", bone, tuning, new Vector3(-8f, 0f, -20f), 1f, matBone, wallsMask, essencePrefab, itemPrefab,
                  Points(points, "A", new Vector3(-13f, 0f, -22f), new Vector3(-2f, 0f, -22f), new Vector3(-8f, 0f, -12f)));
            Enemy("KemikAskeri_B", bone, tuning, new Vector3(9f, 0f, -14f), 1f, matBone, wallsMask, essencePrefab, itemPrefab,
                  Points(points, "B", new Vector3(14f, 0f, -12f), new Vector3(4f, 0f, -12f)));
            Enemy("MagaraKurdu", wolf, tuning, new Vector3(-12f, 0f, -11f), 0.8f, matWolf, wallsMask, essencePrefab, itemPrefab,
                  new Transform[0]);

            // ---- Dinlenme Kampı ----
            BuildRestPoint(new Vector3(0f, 0f, -2.5f), matWall);

            // ---- Boss ----
            var boss = new GameObject("Gorrath");
            boss.transform.SetPositionAndRotation(new Vector3(0f, 0f, 22f), Quaternion.Euler(0f, 180f, 0f));
            boss.transform.localScale = Vector3.one * 1.6f;
            AddCapsule(boss);
            var bossAgent = boss.AddComponent<NavMeshAgent>();
            ConfigureAgent(bossAgent);
            Body(boss.transform, matBoss);
            var bossAim = Child(boss.transform, "AimPoint", new Vector3(0f, 1.2f, 0f));
            var bossCombatant = boss.AddComponent<Combatant>();
            SetEnum(bossCombatant, "team", (int)Team.Enemy);
            SetRef(bossCombatant, "aimPoint", bossAim);
            var bossController = boss.AddComponent<BossController>();
            SetRef(bossController, "definition", gorrath);
            SetRef(bossController, "tuning", tuning);
            SetRef(bossController, "essencePickupPrefab", essencePrefab);
            SetRef(bossController, "itemPickupPrefab", itemPrefab);

            var arena = new GameObject("BossArenaTrigger");
            arena.transform.position = new Vector3(0f, 1.5f, 6f);
            var arenaBox = arena.AddComponent<BoxCollider>();
            arenaBox.isTrigger = true;
            arenaBox.size = new Vector3(8f, 3f, 2f);
            var arenaBody = arena.AddComponent<Rigidbody>();
            arenaBody.isKinematic = true;
            arenaBody.useGravity = false;
            var arenaTrigger = arena.AddComponent<BossArenaTrigger>();
            SetRef(arenaTrigger, "boss", bossController);
            SetRef(arenaTrigger, "entranceBlocker", blocker);

            // ---- Oyun yöneticisi ----
            var gm = new GameObject("GameManager").AddComponent<GameManager>();
            SetRef(gm, "player", playerCombat);

            // ---- Dövüş hissi (ses, hit-stop, sarsıntı, hasar sayıları) ----
            new GameObject("CombatFeedback", typeof(AudioSource), typeof(CombatFeedback));

            // ---- Arayüz ----
            BuildUI(playerCombat, playerInventory, bossController);

            // ---- Kaydet ----
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.DeleteAsset(NavMeshPath);
            AssetDatabase.CreateAsset(surface.navMeshData, NavMeshPath);
            EditorUtility.SetDirty(surface);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            bool ok = Verify(playerCombat, "definition") & Verify(playerCombat, "tuning") &
                      Verify(bossController, "definition") & Verify(bossController, "tuning");
            foreach (var enemy in Object.FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
                ok &= Verify(enemy, "definition") & Verify(enemy, "tuning");

            BuildMainMenu();
            AddToBuildSettings();
            EditorSceneManager.OpenScene(ScenePath);

            if (ok) Debug.Log("[Eryndor] Test sahnesi kuruldu ve kontrol edildi: " + ScenePath + ". Play'e bas.");
            else Debug.LogError("[Eryndor] Sahne kuruldu ama bazı veri bağlantıları boş. Console'daki satırları Claude'a gönder.");
        }

        // ================= Yardımcılar =================

        private static T Load<T>(string fileName) where T : Object =>
            AssetDatabase.LoadAssetAtPath<T>($"{DataDir}/{fileName}.asset");

        private static void SetupAtmosphere()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.22f, 0.22f, 0.27f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.025f;
            RenderSettings.fogColor = new Color(0.05f, 0.05f, 0.07f);
            var light = Object.FindAnyObjectByType<Light>();
            if (light != null)
            {
                light.intensity = 0.8f;
                light.color = new Color(0.75f, 0.8f, 1f);
            }
        }

        private static int EnsureLayer(string layerName)
        {
            int existing = LayerMask.NameToLayer(layerName);
            if (existing >= 0) return existing;

            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tagManager.FindProperty("layers");
            for (int i = 8; i < layers.arraySize; i++)
            {
                var p = layers.GetArrayElementAtIndex(i);
                if (!string.IsNullOrEmpty(p.stringValue)) continue;
                p.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                return i;
            }
            return -1;
        }

        private static Material Mat(string fileName, Color color, bool emissive = false)
        {
            string path = $"{MaterialDir}/{fileName}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var m = new Material(shader);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Color")) m.SetColor("_Color", color);
            if (emissive && m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", color * 2f);
            }
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        private static void SetMaterial(GameObject go, Material m)
        {
            var r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = m;
        }

        private static GameObject Wall(Transform parent, string name, Vector3 position, Vector3 size, Material m, int layer)
        {
            var w = GameObject.CreatePrimitive(PrimitiveType.Cube);
            w.name = name;
            if (parent != null) w.transform.SetParent(parent);
            w.transform.position = position;
            w.transform.localScale = size;
            w.layer = layer;
            SetMaterial(w, m);
            return w;
        }

        /// <summary>Çarpışmasız görsel gövde + yönü gösteren "burun".</summary>
        private static void Body(Transform parent, Material m)
        {
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Govde";
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.transform.SetParent(parent, false);
            body.transform.localPosition = new Vector3(0f, 1f, 0f);
            SetMaterial(body, m);

            var nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nose.name = "Burun";
            Object.DestroyImmediate(nose.GetComponent<Collider>());
            nose.transform.SetParent(parent, false);
            nose.transform.localPosition = new Vector3(0f, 1.5f, 0.45f);
            nose.transform.localScale = new Vector3(0.2f, 0.2f, 0.3f);
            SetMaterial(nose, m);
        }

        private static Transform Child(Transform parent, string name, Vector3 localPosition)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = localPosition;
            return t;
        }

        private static void AddCapsule(GameObject go)
        {
            var col = go.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 1f, 0f);
            col.height = 2f;
            col.radius = 0.45f;
        }

        private static void ConfigureAgent(NavMeshAgent agent)
        {
            agent.radius = 0.45f;
            agent.height = 2f;
            agent.angularSpeed = 360f;
            agent.acceleration = 12f;
            agent.stoppingDistance = 1f;
        }

        private static Transform[] Points(Transform root, string prefix, params Vector3[] positions)
        {
            var result = new Transform[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                result[i] = Child(root, $"{prefix}_{i + 1}", positions[i]);
            }
            return result;
        }

        private static void Enemy(string name, EnemyDefinition definition, CombatTuningAsset tuning, Vector3 position, float scale,
                                  Material m, int wallsMask, EssencePickup essencePrefab, ItemPickup itemPrefab, Transform[] patrol)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            go.transform.localScale = Vector3.one * scale;
            AddCapsule(go);
            ConfigureAgent(go.AddComponent<NavMeshAgent>());
            Body(go.transform, m);
            var eyes = Child(go.transform, "Gozler", new Vector3(0f, 1.6f, 0.2f));
            var aim = Child(go.transform, "AimPoint", new Vector3(0f, 1.2f, 0f));

            var combatant = go.AddComponent<Combatant>();
            SetEnum(combatant, "team", (int)Team.Enemy);
            SetRef(combatant, "aimPoint", aim);

            var controller = go.AddComponent<EnemyController>();
            SetRef(controller, "definition", definition);
            SetRef(controller, "tuning", tuning);
            SetRef(controller, "eyes", eyes);
            SetRef(controller, "essencePickupPrefab", essencePrefab);
            SetRef(controller, "itemPickupPrefab", itemPrefab);
            SetMask(controller, "sightBlockers", wallsMask);
            SetRefArray(controller, "patrolPoints", patrol);

            // Başının üstündeki "?" / "!" göstergesi
            var indicatorGo = new GameObject("Gosterge");
            indicatorGo.transform.SetParent(go.transform, false);
            indicatorGo.transform.localPosition = new Vector3(0f, 2.6f, 0f);
            var tmp = indicatorGo.AddComponent<TextMeshPro>();
            tmp.text = string.Empty;
            tmp.fontSize = 8f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.rectTransform.sizeDelta = new Vector2(2f, 1f);
            var indicator = indicatorGo.AddComponent<EnemyIndicator>();
            SetRef(indicator, "enemy", controller);
            SetRef(indicator, "label", tmp);
        }

        private static EssencePickup CreateEssencePrefab(Material m)
        {
            string path = PrefabDir + "/EssencePickup.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<EssencePickup>(path);
            if (existing != null) return existing;

            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "EssencePickup";
            go.transform.localScale = Vector3.one * 0.45f;
            SetMaterial(go, m);
            go.GetComponent<Collider>().isTrigger = true;
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            go.AddComponent<EssencePickup>();

            var glow = new GameObject("Isik").AddComponent<Light>();
            glow.transform.SetParent(go.transform, false);
            glow.type = LightType.Point;
            glow.color = new Color(0.45f, 0.75f, 1f);
            glow.range = 4f;
            glow.intensity = 3f;

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<EssencePickup>();
        }

        private static ItemPickup CreateItemPrefab(Material m)
        {
            string path = PrefabDir + "/ItemPickup.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<ItemPickup>(path);
            if (existing != null) return existing;

            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "ItemPickup";
            go.transform.localScale = Vector3.one * 0.35f;
            SetMaterial(go, m);
            var box = (BoxCollider)go.GetComponent<Collider>();
            box.isTrigger = true;
            box.size = Vector3.one * 3f;
            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            go.AddComponent<ItemPickup>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab.GetComponent<ItemPickup>();
        }

        // ---------- Arayüz ----------

        private static void BuildUI(PlayerCombat player, PlayerInventory inventory, BossController boss)
        {
            var canvasGo = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var root = canvasGo.transform;

            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var topLeft = new Vector2(0f, 1f);
            var hp = Bar(root, "Can", topLeft, new Vector2(30f, -30f), new Vector2(380f, 22f), new Color(0.75f, 0.15f, 0.15f));
            var mana = Bar(root, "Mana", topLeft, new Vector2(30f, -60f), new Vector2(380f, 16f), new Color(0.25f, 0.45f, 0.95f));
            var stamina = Bar(root, "Stamina", topLeft, new Vector2(30f, -84f), new Vector2(380f, 12f), new Color(0.9f, 0.75f, 0.2f));

            var bottomLeft = Vector2.zero;
            var essenceLabels = new TMP_Text[3];
            for (int i = 0; i < 3; i++)
            {
                essenceLabels[i] = Label(root, $"EssenceSlot_{i + 1}", "—", bottomLeft, new Vector2(30f + i * 200f, 30f),
                                         new Vector2(190f, 64f), 20f, TextAlignmentOptions.BottomLeft);
            }
            var empowerFill = new GameObject("GuclendirmeBekleme", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            Place(empowerFill.rectTransform, root, bottomLeft, new Vector2(30f, 104f), new Vector2(590f, 8f));
            empowerFill.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            empowerFill.type = Image.Type.Filled;
            empowerFill.fillMethod = Image.FillMethod.Horizontal;
            empowerFill.fillAmount = 0f;
            empowerFill.color = new Color(0.45f, 0.75f, 1f, 0.8f);

            var bottomRight = new Vector2(1f, 0f);
            var inventoryLabels = new TMP_Text[4];
            for (int i = 0; i < 4; i++)
            {
                inventoryLabels[i] = Label(root, $"Envanter_{i + 1}", $"{i + 1}", bottomRight,
                                           new Vector2(-30f - (3 - i) * 210f, 30f), new Vector2(200f, 40f), 18f, TextAlignmentOptions.BottomRight);
            }

            var topCenter = new Vector2(0.5f, 1f);
            var bossPanel = new GameObject("BossPaneli", typeof(RectTransform));
            Place((RectTransform)bossPanel.transform, root, topCenter, new Vector2(0f, -30f), new Vector2(820f, 70f));
            var bossName = Label(bossPanel.transform, "BossAdi", "Gorrath", topCenter, Vector2.zero, new Vector2(820f, 34f), 26f, TextAlignmentOptions.Center);
            var bossBar = Bar(bossPanel.transform, "BossCan", topCenter, new Vector2(0f, -40f), new Vector2(820f, 20f), new Color(0.6f, 0.1f, 0.1f));
            bossPanel.SetActive(false);

            var bark = Label(root, "Bark", string.Empty, new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(1300f, 60f), 28f, TextAlignmentOptions.Center);
            var prompt = Label(root, "Ipucu", string.Empty, new Vector2(0.5f, 0f), new Vector2(0f, 220f), new Vector2(900f, 44f), 26f, TextAlignmentOptions.Center);
            prompt.color = new Color(1f, 0.9f, 0.6f);

            var hud = canvasGo.AddComponent<HUDController>();
            SetRef(hud, "player", player);
            SetRef(hud, "inventory", inventory);
            SetRef(hud, "boss", boss);
            SetRef(hud, "healthBar", hp);
            SetRef(hud, "manaBar", mana);
            SetRef(hud, "staminaBar", stamina);
            SetRefArray(hud, "essenceSlotLabels", essenceLabels);
            SetRef(hud, "empowerCooldownFill", empowerFill);
            SetRefArray(hud, "inventoryLabels", inventoryLabels);
            SetRef(hud, "bossPanel", bossPanel);
            SetRef(hud, "bossHealthBar", bossBar);
            SetRef(hud, "bossNameLabel", bossName);
            SetRef(hud, "barkLabel", bark);
            SetRef(hud, "promptLabel", prompt);

            // Essence değiştirme paneli
            var swapUi = canvasGo.AddComponent<EssenceSwapUI>();
            var swapPanel = new GameObject("EssencePaneli", typeof(RectTransform), typeof(Image));
            Place((RectTransform)swapPanel.transform, root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 460f));
            ((RectTransform)swapPanel.transform).pivot = new Vector2(0.5f, 0.5f);
            swapPanel.GetComponent<Image>().color = new Color(0.04f, 0.05f, 0.08f, 0.94f);
            var swapBody = Label(swapPanel.transform, "Metin", string.Empty, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, 28f, TextAlignmentOptions.Center);
            Stretch(swapBody.rectTransform);
            swapBody.margin = new Vector4(40f, 30f, 40f, 30f);
            swapPanel.SetActive(false);
            SetRef(swapUi, "player", player);
            SetRef(swapUi, "panel", swapPanel);
            SetRef(swapUi, "body", swapBody);

            // Zafer / ölüm ekranı
            var endScreen = canvasGo.AddComponent<EndScreenUI>();
            var endPanel = FullPanel(root, "SonEkrani", new Color(0f, 0f, 0f, 0.8f));
            var center = new Vector2(0.5f, 0.5f);
            var title = Label(endPanel.transform, "Baslik", "", center, new Vector2(0f, 120f), new Vector2(1200f, 90f), 54f, TextAlignmentOptions.Center);
            var stats = Label(endPanel.transform, "Istatistikler", "", center, new Vector2(0f, -10f), new Vector2(700f, 170f), 28f, TextAlignmentOptions.Center);
            MakeButton(endPanel.transform, "Tekrar Dene", new Vector2(-140f, -170f), endScreen.Restart);
            MakeButton(endPanel.transform, "Ana Menü", new Vector2(140f, -170f), endScreen.QuitToMenu);
            endPanel.SetActive(false);

            // Duraklatma ekranı
            var pausePanel = FullPanel(root, "Duraklatma", new Color(0f, 0f, 0f, 0.6f));
            Label(pausePanel.transform, "Baslik", "Duraklatıldı", center, new Vector2(0f, 80f), new Vector2(800f, 80f), 48f, TextAlignmentOptions.Center);
            MakeButton(pausePanel.transform, "Devam", new Vector2(-140f, -40f), endScreen.Resume);
            MakeButton(pausePanel.transform, "Baştan Başla", new Vector2(140f, -40f), endScreen.Restart);
            pausePanel.SetActive(false);

            SetRef(endScreen, "panel", endPanel);
            SetRef(endScreen, "titleLabel", title);
            SetRef(endScreen, "statsLabel", stats);
            SetRef(endScreen, "pausePanel", pausePanel);
        }

        private static void Place(RectTransform rt, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rt.SetParent(parent, false);
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static TextMeshProUGUI Label(Transform parent, string name, string text, Vector2 anchor, Vector2 position,
                                             Vector2 size, float fontSize, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform));
            Place((RectTransform)go.transform, parent, anchor, position, size);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = fontSize;
            t.alignment = alignment;
            t.color = Color.white;
            t.raycastTarget = false;
            return t;
        }

        private static Slider Bar(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size, Color fillColor)
        {
            var go = DefaultControls.CreateSlider(new DefaultControls.Resources());
            go.name = name;
            Place((RectTransform)go.transform, parent, anchor, position, size);

            var slider = go.GetComponent<Slider>();
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;
            var handleArea = go.transform.Find("Handle Slide Area");
            if (handleArea != null) Object.DestroyImmediate(handleArea.gameObject);
            slider.handleRect = null;

            var background = go.transform.Find("Background") as RectTransform;
            if (background != null)
            {
                Stretch(background);
                background.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
            }
            var fillArea = go.transform.Find("Fill Area") as RectTransform;
            if (fillArea != null) Stretch(fillArea);
            var fill = go.transform.Find("Fill Area/Fill") as RectTransform;
            if (fill != null)
            {
                fill.sizeDelta = Vector2.zero;
                fill.GetComponent<Image>().color = fillColor;
            }

            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            return slider;
        }

        private static GameObject FullPanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            Stretch(rt);
            go.GetComponent<Image>().color = color;
            return go;
        }

        private static void MakeButton(Transform parent, string text, Vector2 position, UnityAction onClick)
        {
            var go = new GameObject(text, typeof(RectTransform), typeof(Image), typeof(Button));
            Place((RectTransform)go.transform, parent, new Vector2(0.5f, 0.5f), position, new Vector2(240f, 60f));
            go.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0.5f);
            go.GetComponent<Image>().color = new Color(0.22f, 0.2f, 0.19f, 1f);
            UnityEventTools.AddPersistentListener(go.GetComponent<Button>().onClick, onClick);

            var label = Label(go.transform, "Yazi", text, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero, 26f, TextAlignmentOptions.Center);
            Stretch(label.rectTransform);
        }

        // ---------- SerializedObject ile private alanlara değer yazma ----------

        private static SerializedProperty Find(SerializedObject so, string field)
        {
            var p = so.FindProperty(field);
            if (p == null) Debug.LogError($"[Eryndor] Alan bulunamadı: {so.targetObject.GetType().Name}.{field}");
            return p;
        }

        private static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var p = Find(so, field);
            if (p == null) return;
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetRefArray(Object target, string field, IReadOnlyList<Object> values)
        {
            var so = new SerializedObject(target);
            var p = Find(so, field);
            if (p == null) return;
            p.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetEnum(Object target, string field, int index)
        {
            var so = new SerializedObject(target);
            var p = Find(so, field);
            if (p == null) return;
            p.enumValueIndex = index;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetMask(Object target, string field, int mask)
        {
            var so = new SerializedObject(target);
            var p = Find(so, field);
            if (p == null) return;
            p.intValue = mask;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetStartingItems(PlayerInventory inventory, params (ItemDefinition item, int count)[] items)
        {
            var so = new SerializedObject(inventory);
            var p = Find(so, "startingItems");
            if (p == null) return;
            p.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++)
            {
                var element = p.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("item").objectReferenceValue = items[i].item;
                element.FindPropertyRelative("count").intValue = items[i].count;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildRestPoint(Vector3 position, Material stone)
        {
            var altar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            altar.name = "DinlenmeSunagi";
            altar.transform.position = position + new Vector3(0f, 0.5f, 0f);
            altar.transform.localScale = new Vector3(1.2f, 0.5f, 1.2f);
            SetMaterial(altar, stone);

            var flame = new GameObject("Alev").AddComponent<Light>();
            flame.transform.position = position + new Vector3(0f, 1.6f, 0f);
            flame.type = LightType.Point;
            flame.color = new Color(1f, 0.6f, 0.25f);
            flame.range = 9f;
            flame.intensity = 4f;

            var rest = altar.AddComponent<RestPoint>();
            SetRef(rest, "flame", flame);
        }

        private static bool Verify(Object target, string field)
        {
            var p = new SerializedObject(target).FindProperty(field);
            if (p != null && p.objectReferenceValue != null) return true;
            Debug.LogError($"[Eryndor] Boş bağlantı: {target.name} > {target.GetType().Name}.{field}", target);
            return false;
        }

        [MenuItem("Eryndor/Ana Menüyü Kur")]
        public static void BuildMainMenuMenuItem()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildMainMenu();
            AddToBuildSettings();
            EditorSceneManager.OpenScene(MenuScenePath);
        }

        private static void BuildMainMenu()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            var cam = Camera.main;
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.04f, 0.04f, 0.06f);
            }

            var canvasGo = new GameObject("Menu", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            var root = canvasGo.transform;
            var center = new Vector2(0.5f, 0.5f);
            var title = Label(root, "Baslik", "ERYNDOR", center, new Vector2(0f, 200f), new Vector2(1200f, 140f), 120f, TextAlignmentOptions.Center);
            title.characterSpacing = 25f;
            title.color = new Color(0.9f, 0.85f, 0.75f);
            var subtitle = Label(root, "AltBaslik", "İlk Kat", center, new Vector2(0f, 95f), new Vector2(800f, 60f), 42f, TextAlignmentOptions.Center);
            subtitle.color = new Color(0.55f, 0.75f, 1f);
            Label(root, "Alinti", "Silahsız in. Essence'ı al. Bekçiyi yen.", center, new Vector2(0f, 30f), new Vector2(1000f, 40f), 24f, TextAlignmentOptions.Center)
                .color = new Color(0.7f, 0.7f, 0.7f);

            var menu = canvasGo.AddComponent<MainMenuUI>();
            MakeButton(root, "Başla", new Vector2(0f, -90f), menu.Play);
            MakeButton(root, "Çıkış", new Vector2(0f, -170f), menu.Quit);

            Label(root, "Kontroller",
                  "WASD hareket · Fare kamera · Sol tık hafif · Sağ tık ağır · Space counter · Shift kaçış · Q/E teknik · R Güçlendirme · F etkileşim · 1-4 eşya",
                  new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(1800f, 40f), 20f, TextAlignmentOptions.Center)
                .color = new Color(0.6f, 0.6f, 0.6f);

            EditorSceneManager.SaveScene(scene, MenuScenePath);
        }

        private static void AddToBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            scenes.RemoveAll(s => s.path == ScenePath || s.path == MenuScenePath);
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            if (File.Exists(MenuScenePath)) scenes.Insert(0, new EditorBuildSettingsScene(MenuScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
