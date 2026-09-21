#if UNITY_EDITOR
using System.IO;
using FracturedChorus.VFX;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Editor
{
    public static class ButterflyTransitionPrefabBuilder
    {
        public const string PrefabPath = "Assets/FracturedChorus/VFX/ButterflyTransition/Prefabs/FC_ButterflyTransition.prefab";
        public const string MaterialPath = "Assets/FracturedChorus/VFX/ButterflyTransition/Materials/FC_UI_Additive.mat";
        private const string ShaderPath = "Assets/FracturedChorus/VFX/ButterflyTransition/Materials/FC_UI_Additive.shader";
        private const string SpriteRoot = "Assets/FracturedChorus/VFX/ButterflyTransition/Sprites/";
        private const string ParticleRoot = "Assets/FracturedChorus/VFX/ButterflyTransition/Particles/";


        [MenuItem("Fractured Chorus/VFX/Create Butterfly Transition Prefab")]
        public static void CreatePrefab()
        {
            try
            {
                EnsureFolders();
                ConfigureSprites();
                var material = EnsureMaterial();
                var go = new GameObject("FC_ButterflyTransition", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup), typeof(ButterflyBezierFlight), typeof(ButterflyWingAnimator), typeof(ButterflyTrailController), typeof(ButterflyTransitionController));
                var controller = go.GetComponent<ButterflyTransitionController>();
                ButterflyTransitionHierarchy.EnsureMissing(controller);
                Wire(controller, material);
                var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
                Object.DestroyImmediate(go);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                if (prefab != null)
                {
                    Selection.activeObject = prefab;
                }

                Debug.Log("[Fractured Chorus] Butterfly transition prefab: " + PrefabPath);
            }
            catch (System.Exception error)
            {
                Debug.LogError("Failed to create butterfly transition prefab: " + error);
                EditorUtility.DisplayDialog("Butterfly Transition", "Create prefab failed. Check Console.", "OK");
            }
        }

        [MenuItem("Fractured Chorus/VFX/Add Butterfly Transition To Open Scene")]
        public static void AddToOpenScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Butterfly Transition", "Exit Play Mode rồi chạy lại.", "OK");
                return;
            }

            var existing = Object.FindObjectsByType<ButterflyTransitionController>(FindObjectsInactive.Include);
            if (existing.Length > 0)
            {
                ButterflyTransitionHierarchy.EnsureMissing(existing[0]);
                Selection.activeGameObject = existing[0].gameObject;
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(existing[0].gameObject.scene);
                Debug.Log("[Fractured Chorus] FC_ButterflyTransition đã có trên scene. Chỉ bổ sung object thiếu. Ctrl+S nếu Hierarchy vừa đổi.");
                return;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab != null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                instance.name = ButterflyTransitionHierarchy.RootName;
                Selection.activeGameObject = instance;
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(instance.scene);
                return;
            }

            var go = new GameObject(ButterflyTransitionHierarchy.RootName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup), typeof(ButterflyBezierFlight), typeof(ButterflyWingAnimator), typeof(ButterflyTrailController), typeof(ButterflyTransitionController));
            var controller = go.GetComponent<ButterflyTransitionController>();
            ButterflyTransitionHierarchy.EnsureMissing(controller);
            var material = EnsureMaterial();
            Wire(controller, material);
            Selection.activeGameObject = go;
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(go.scene);
        }

        [MenuItem("Fractured Chorus/VFX/Ping Butterfly Transition Prefab")]
        public static void PingPrefab()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogError("[Fractured Chorus] Missing " + PrefabPath);
                return;
            }

            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
        }

        public static void BatchCreatePrefab()
        {
            CreatePrefab();
            EditorApplication.Exit(0);
        }

        private static void Wire(ButterflyTransitionController controller, Material additive)
        {
            var wings = new[]
            {
                LoadSprite(SpriteRoot + "fc_bt_wing_01_closed.png"),
                LoadSprite(SpriteRoot + "fc_bt_wing_02_quarter.png"),
                LoadSprite(SpriteRoot + "fc_bt_wing_03_half.png"),
                LoadSprite(SpriteRoot + "fc_bt_wing_04_open.png")
            };
            var prisms = new[]
            {
                LoadSprite(ParticleRoot + "fc_bt_trail_triangle_lg.png"),
                LoadSprite(ParticleRoot + "fc_bt_trail_triangle_sm.png"),
                LoadSprite(ParticleRoot + "fc_bt_trail_diamond.png"),
                LoadSprite(ParticleRoot + "fc_bt_trail_shard.png")
            };
            var notes = new[]
            {
                LoadSprite(ParticleRoot + "fc_bt_trail_note.png"),
                LoadSprite(ParticleRoot + "fc_bt_trail_freq.png"),
                LoadSprite(ParticleRoot + "fc_bt_trail_waveform.png")
            };
            var star = LoadSprite(ParticleRoot + "fc_bt_trail_star_dust.png");
            var glitter = LoadSprite(ParticleRoot + "fc_bt_trail_glitter.png");
            var bloom = LoadSprite(ParticleRoot + "fc_bt_glow_bloom.png");

            var root = controller.transform;
            var butterflyRoot = root.Find(ButterflyTransitionHierarchy.ButterflyRootName) as RectTransform;
            var sprite = root.Find(ButterflyTransitionHierarchy.ButterflyRootName + "/" + ButterflyTransitionHierarchy.ButterflySpriteName)?.GetComponent<Image>();
            var glow = root.Find(ButterflyTransitionHierarchy.ButterflyRootName + "/" + ButterflyTransitionHierarchy.ButterflyGlowName)?.GetComponent<Image>();
            var origin = root.Find(ButterflyTransitionHierarchy.ButterflyRootName + "/" + ButterflyTransitionHierarchy.TrailOriginName) as RectTransform;
            var fade = root.Find(ButterflyTransitionHierarchy.FadeOverlayName)?.GetComponent<CanvasGroup>();
            if (sprite != null && wings[3] != null)
            {
                sprite.sprite = wings[3];
                sprite.material = additive;
            }

            if (glow != null)
            {
                glow.sprite = bloom;
                glow.color = new Color(0.55f, 0.85f, 1f, 0.35f);
                glow.material = additive;
            }

            var trail = controller.GetComponent<ButterflyTrailController>();
            trail.Bind(
                root.Find(ButterflyTransitionHierarchy.VfxName + "/" + ButterflyTransitionHierarchy.StarDustName) as RectTransform,
                root.Find(ButterflyTransitionHierarchy.VfxName + "/" + ButterflyTransitionHierarchy.SmallFragmentsName) as RectTransform,
                root.Find(ButterflyTransitionHierarchy.VfxName + "/" + ButterflyTransitionHierarchy.PrismFragmentsName) as RectTransform,
                root.Find(ButterflyTransitionHierarchy.VfxName + "/" + ButterflyTransitionHierarchy.MusicFragmentsName) as RectTransform,
                root.Find(ButterflyTransitionHierarchy.VfxName + "/" + ButterflyTransitionHierarchy.WaveformTrailsName) as RectTransform,
                star,
                glitter,
                prisms,
                notes,
                additive);

            var so = new SerializedObject(controller);
            Assign(so, "butterflySprite", sprite);
            Assign(so, "butterflyGlow", glow);
            Assign(so, "butterflyRoot", butterflyRoot);
            Assign(so, "trailOrigin", origin);
            Assign(so, "rootGroup", controller.GetComponent<CanvasGroup>());
            Assign(so, "fadeOverlay", fade);
            AssignSprites(so, "wingFrames", wings);
            so.FindProperty("autoPlay").boolValue = false;
            so.FindProperty("playOnEnable").boolValue = false;
            so.FindProperty("duration").floatValue = 4.2f;
            var hold = so.FindProperty("holdAtEnd");
            if (hold != null)
            {
                hold.boolValue = true;
            }

            var aura = so.FindProperty("hoverAuraEmission");
            if (aura != null)
            {
                aura.floatValue = 0.45f;
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            var flight = controller.GetComponent<ButterflyBezierFlight>();
            flight.Bind(controller.transform as RectTransform, butterflyRoot);
        }

        private static void Assign(SerializedObject so, string field, Object value)
        {
            var prop = so.FindProperty(field);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
            }
        }

        private static void AssignSprites(SerializedObject so, string field, Sprite[] sprites)
        {
            var prop = so.FindProperty(field);
            if (prop == null)
            {
                return;
            }

            prop.arraySize = sprites.Length;
            for (var i = 0; i < sprites.Length; i++)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
            }
        }

        private static Material EnsureMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (existing != null)
            {
                return existing;
            }

            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath) ?? Shader.Find("FracturedChorus/UI/Additive");
            if (shader == null)
            {
                shader = Shader.Find("UI/Default");
            }

            var material = new Material(shader)
            {
                name = "FC_UI_Additive"
            };
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }

        private static Sprite LoadSprite(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void ConfigureSprites()
        {
            ConfigureFolder(SpriteRoot);
            ConfigureFolder(ParticleRoot);
        }

        private static void ConfigureFolder(string folder)
        {
            var full = Path.Combine(Directory.GetParent(Application.dataPath).FullName, folder.Replace('/', Path.DirectorySeparatorChar));
            if (!Directory.Exists(full))
            {
                return;
            }

            foreach (var file in Directory.GetFiles(full, "*.png"))
            {
                var assetPath = folder + Path.GetFileName(file);
                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.alphaIsTransparency = true;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.maxTextureSize = 2048;
                importer.SaveAndReimport();
            }
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets/FracturedChorus/VFX");
            EnsureFolder("Assets/FracturedChorus/VFX/ButterflyTransition");
            EnsureFolder("Assets/FracturedChorus/VFX/ButterflyTransition/Sprites");
            EnsureFolder("Assets/FracturedChorus/VFX/ButterflyTransition/Particles");
            EnsureFolder("Assets/FracturedChorus/VFX/ButterflyTransition/Materials");
            EnsureFolder("Assets/FracturedChorus/VFX/ButterflyTransition/Prefabs");
            EnsureFolder("Assets/FracturedChorus/VFX/ButterflyTransition/Animations");
            EnsureFolder("Assets/FracturedChorus/VFX/ButterflyTransition/Scripts");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(name))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }
}
#endif
