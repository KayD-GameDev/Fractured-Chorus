#if UNITY_EDITOR
using FracturedChorus.Data;
using FracturedChorus.RunMap;
using FracturedChorus.RunMap.Core;
using FracturedChorus.RunMap.UI;
using UnityEditor;
using UnityEngine;

namespace FracturedChorus.Editor
{
    public sealed class PinkyVaultMapEditorWindow : EditorWindow
    {
        private const string ConfigPath = "Assets/FracturedChorus/Data/ScriptableObjects/Presets/PinkyVaultConfig_Default.asset";

        private PinkyVaultConfigSO _config;
        private SerializedObject _serialized;
        private Vector2 _scroll;
        private int _selectedSector;

        [MenuItem("Fractured Chorus/Run Map/Open Pinky Vault Map Editor", false, 31)]
        public static void Open()
        {
            var window = GetWindow<PinkyVaultMapEditorWindow>("Pinky Vault Map");
            window.minSize = new Vector2(440f, 560f);
            window.Show();
        }

        private void OnEnable()
        {
            LoadOrCreateConfig();
        }

        private void OnGUI()
        {
            if (_config == null)
            {
                LoadOrCreateConfig();
            }

            if (_config == null)
            {
                EditorGUILayout.HelpBox("Không tạo được PinkyVaultConfigSO.", MessageType.Error);
                return;
            }

            _serialized ??= new SerializedObject(_config);
            _serialized.Update();

            EditorGUILayout.LabelField("Pinky Vault — Inner Map Node (3 Parts)", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);

            DrawConfigToolbar();
            EditorGUILayout.Space(8f);

            var tabs = new[] { "Part 1 · Pulse", "Part 2 · Echo", "Part 3 · Canticle" };
            _selectedSector = GUILayout.Toolbar(_selectedSector, tabs);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            DrawSectorEditor(_selectedSector);
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(8f);
            DrawActions();

            _serialized.ApplyModifiedProperties();
        }

        private void DrawConfigToolbar()
        {
            EditorGUILayout.BeginHorizontal();
            var picked = (PinkyVaultConfigSO)EditorGUILayout.ObjectField("Config Asset", _config, typeof(PinkyVaultConfigSO), false);
            if (picked != _config)
            {
                _config = picked;
                _serialized = _config != null ? new SerializedObject(_config) : null;
            }

            if (GUILayout.Button("Reload", GUILayout.Width(64f)))
            {
                LoadOrCreateConfig();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSectorEditor(int sectorIndex)
        {
            var propName = sectorIndex switch
            {
                0 => "pulse",
                1 => "echo",
                2 => "canticle",
                _ => "pulse"
            };

            var sectorProp = _serialized.FindProperty(propName);
            if (sectorProp == null)
            {
                return;
            }

            EditorGUILayout.PropertyField(sectorProp.FindPropertyRelative("title"));
            EditorGUILayout.PropertyField(sectorProp.FindPropertyRelative("bossLabel"));
            EditorGUILayout.Space(4f);

            EditorGUILayout.LabelField("Grid", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(sectorProp.FindPropertyRelative("columnCount"));
            EditorGUILayout.PropertyField(sectorProp.FindPropertyRelative("floorCount"));
            EditorGUILayout.PropertyField(sectorProp.FindPropertyRelative("bossFloor"));
            EditorGUILayout.PropertyField(sectorProp.FindPropertyRelative("pathCount"));
            EditorGUILayout.PropertyField(sectorProp.FindPropertyRelative("previewSeed"));
            EditorGUILayout.PropertyField(sectorProp.FindPropertyRelative("loadBossScene"));

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Node Weights", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(sectorProp.FindPropertyRelative("battleWeight"));
            EditorGUILayout.PropertyField(sectorProp.FindPropertyRelative("eliteWeight"));
            EditorGUILayout.PropertyField(sectorProp.FindPropertyRelative("eventWeight"));
            EditorGUILayout.PropertyField(sectorProp.FindPropertyRelative("relayWeight"));
            EditorGUILayout.PropertyField(sectorProp.FindPropertyRelative("campWeight"));
            EditorGUILayout.PropertyField(sectorProp.FindPropertyRelative("treasureWeight"));

            var floor = sectorProp.FindPropertyRelative("floorCount").intValue;
            var boss = sectorProp.FindPropertyRelative("bossFloor").intValue;
            EditorGUILayout.HelpBox(
                $"Runtime: Map {sectorIndex + 1}/3 · F1–F{floor} → Boss F{boss} ({sectorProp.FindPropertyRelative("bossLabel").stringValue})" +
                (sectorProp.FindPropertyRelative("loadBossScene").boolValue ? " · load combat scene" : " · stub clear → next map"),
                MessageType.None);
        }

        private void DrawActions()
        {
            EditorGUILayout.LabelField("Preview (Edit Mode)", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Preview Part 1"))
            {
                PreviewSector(PinkySectorId.Pulse);
            }

            if (GUILayout.Button("Preview Part 2"))
            {
                PreviewSector(PinkySectorId.Echo);
            }

            if (GUILayout.Button("Preview Part 3"))
            {
                PreviewSector(PinkySectorId.Canticle);
            }

            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Back to Macro Map"))
            {
                ShowMacroMapInEditor();
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Config"))
            {
                SaveConfig();
            }

            if (GUILayout.Button("Wire To Scene"))
            {
                WireConfigToScene();
            }

            if (GUILayout.Button("Reset Defaults"))
            {
                if (EditorUtility.DisplayDialog("Reset", "Reset cả 3 Part về mặc định?", "Reset", "Cancel"))
                {
                    _config.pulse = PinkyVaultConfigSO.SectorConfig.Default(PinkySectorId.Pulse);
                    _config.echo = PinkyVaultConfigSO.SectorConfig.Default(PinkySectorId.Echo);
                    _config.canticle = PinkyVaultConfigSO.SectorConfig.Default(PinkySectorId.Canticle);
                    EditorUtility.SetDirty(_config);
                    _serialized = new SerializedObject(_config);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void PreviewSector(PinkySectorId sector)
        {
            SaveConfig();
            PrepareInnerMapVisible();

            var controller = Object.FindAnyObjectByType<RunMapController>();
            if (controller == null)
            {
                EditorUtility.DisplayDialog("Preview", "Không tìm thấy RunMapController. Chạy Run Map → Setup Scene Hierarchy.", "OK");
                return;
            }

            var cfg = _config.GetSector(sector);
            var graph = MapGenerator.GenerateSector(sector, cfg.previewSeed, _config.WeightsFor(sector), _config);

            controller.enabled = true;
            controller.Initialize(graph, cfg.previewSeed);

            var cadence = Object.FindAnyObjectByType<CadenceMapController>();
            if (cadence != null)
            {
                var so = new SerializedObject(cadence);
                so.FindProperty("pinkyVaultConfig").objectReferenceValue = _config;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log($"[Fractured Chorus] Preview {cfg.title} — F1..F{cfg.floorCount} → F{cfg.bossFloor} ({cfg.bossLabel})");
        }

        private static void PrepareInnerMapVisible()
        {
            var macro = GameObject.Find("MacroMapLayer");
            var inner = GameObject.Find("InnerMapLayer");
            var scroll = GameObject.Find("MapScrollView");
            var legend = GameObject.Find("LegendPanel");

            if (macro != null)
            {
                macro.SetActive(false);
            }

            if (inner != null)
            {
                inner.SetActive(true);
            }

            if (scroll != null)
            {
                scroll.SetActive(true);
            }

            if (legend != null)
            {
                legend.SetActive(true);
            }
        }

        private static void ShowMacroMapInEditor()
        {
            var macro = GameObject.Find("MacroMapLayer");
            var inner = GameObject.Find("InnerMapLayer");

            if (macro != null)
            {
                macro.SetActive(true);
            }

            if (inner != null)
            {
                inner.SetActive(false);
            }

            var macroView = Object.FindAnyObjectByType<CadenceMacroMapView>();
            macroView?.Build(AssetDatabase.LoadAssetAtPath<CadenceMapLayoutSO>(
                "Assets/FracturedChorus/Data/ScriptableObjects/Presets/CadenceMapLayout_Default.asset"));
        }

        private void WireConfigToScene()
        {
            SaveConfig();
            var cadence = Object.FindAnyObjectByType<CadenceMapController>();
            if (cadence == null)
            {
                EditorUtility.DisplayDialog("Wire", "Không tìm thấy CadenceMapController.", "OK");
                return;
            }

            var so = new SerializedObject(cadence);
            so.FindProperty("pinkyVaultConfig").objectReferenceValue = _config;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(cadence);
            Debug.Log("[Fractured Chorus] PinkyVaultConfig wired to CadenceMapController.");
        }

        private void LoadOrCreateConfig()
        {
            _config = AssetDatabase.LoadAssetAtPath<PinkyVaultConfigSO>(ConfigPath);
            if (_config != null)
            {
                _serialized = new SerializedObject(_config);
                return;
            }

            if (!AssetDatabase.IsValidFolder("Assets/FracturedChorus/Data/ScriptableObjects/Presets"))
            {
                AssetDatabase.CreateFolder("Assets/FracturedChorus/Data", "ScriptableObjects");
                AssetDatabase.CreateFolder("Assets/FracturedChorus/Data/ScriptableObjects", "Presets");
            }

            _config = ScriptableObject.CreateInstance<PinkyVaultConfigSO>();
            _config.pulse = PinkyVaultConfigSO.SectorConfig.Default(PinkySectorId.Pulse);
            _config.echo = PinkyVaultConfigSO.SectorConfig.Default(PinkySectorId.Echo);
            _config.canticle = PinkyVaultConfigSO.SectorConfig.Default(PinkySectorId.Canticle);
            AssetDatabase.CreateAsset(_config, ConfigPath);
            AssetDatabase.SaveAssets();
            _serialized = new SerializedObject(_config);
        }

        private void SaveConfig()
        {
            if (_config == null)
            {
                return;
            }

            EditorUtility.SetDirty(_config);
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
