#if UNITY_EDITOR
using FracturedChorus.Data;
using FracturedChorus.RunMap;
using FracturedChorus.RunMap.UI;
using UnityEditor;
using UnityEngine;

namespace FracturedChorus.Editor
{
    public sealed class CadenceMapLayoutEditorWindow : EditorWindow
    {
        private const string DefaultLayoutPath = "Assets/FracturedChorus/Data/ScriptableObjects/Presets/CadenceMapLayout_Default.asset";
        private const string BackgroundPath = "Assets/FracturedChorus/Art/Backgrounds/cadence_macro_map_bg_v2_5fingers.png";

        private CadenceMapLayoutSO _layout;
        private SerializedObject _serializedLayout;
        private Vector2 _scroll;
        private int _selectedTerritory;
        private bool _showVertices = true;
        private bool _editModePreview;

        [MenuItem("Fractured Chorus/Run Map/Open Layout Editor", false, 30)]
        public static void Open()
        {
            var window = GetWindow<CadenceMapLayoutEditorWindow>("Cadence Map");
            window.minSize = new Vector2(420f, 520f);
            window.Show();
        }

        private void OnEnable()
        {
            LoadOrCreateLayout();
            SceneView.duringSceneGui += OnSceneGUI;
            CadenceMapMaskEditSession.LayoutChanged += OnPreviewLayoutChanged;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            CadenceMapMaskEditSession.LayoutChanged -= OnPreviewLayoutChanged;
            CadenceMapMaskEditSession.PreviewEnabled = false;
            CadenceMapMaskEditSession.Reset();
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            CadenceMapMaskEditSession.SelectedTerritory = _selectedTerritory;
            CadenceMapMaskScenePreview.Draw(sceneView);
        }

        private void OnPreviewLayoutChanged()
        {
            _serializedLayout = _layout != null ? new SerializedObject(_layout) : null;
            RefreshScenePreview();
            Repaint();
        }

        private void OnGUI()
        {
            if (_layout == null)
            {
                LoadOrCreateLayout();
            }

            if (_layout == null)
            {
                EditorGUILayout.HelpBox("Không tạo được CadenceMapLayoutSO.", MessageType.Error);
                return;
            }

            _serializedLayout ??= new SerializedObject(_layout);
            _serializedLayout.Update();

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Cadence Macro Map Layout", EditorStyles.boldLabel);
            EditorGUILayout.Space(4f);

            DrawAssetToolbar();
            EditorGUILayout.Space(8f);
            DrawPreviewSection();
            EditorGUILayout.Space(8f);
            DrawBackgroundSection();
            EditorGUILayout.Space(8f);
            DrawTerritorySection();
            EditorGUILayout.Space(8f);
            DrawActionButtons();

            _serializedLayout.ApplyModifiedProperties();
        }

        private void DrawAssetToolbar()
        {
            EditorGUILayout.BeginHorizontal();
            var picked = (CadenceMapLayoutSO)EditorGUILayout.ObjectField("Layout Asset", _layout, typeof(CadenceMapLayoutSO), false);
            if (picked != _layout)
            {
                _layout = picked;
                _serializedLayout = _layout != null ? new SerializedObject(_layout) : null;
                _selectedTerritory = 0;
            }

            if (GUILayout.Button("Reload", GUILayout.Width(72f)))
            {
                LoadOrCreateLayout();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawPreviewSection()
        {
            EditorGUILayout.LabelField("Edit Mode Preview", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            _editModePreview = EditorGUILayout.Toggle("Scene View Mask Edit", _editModePreview);
            if (EditorGUI.EndChangeCheck())
            {
                CadenceMapMaskEditSession.PreviewEnabled = _editModePreview;
                CadenceMapMaskEditSession.Layout = _layout;
                CadenceMapMaskEditSession.SelectedTerritory = _selectedTerritory;

                if (_editModePreview)
                {
                    PrepareEditModeScene();
                    RefreshScenePreview();
                }

                SceneView.RepaintAll();
            }

            if (_editModePreview)
            {
                EditorGUILayout.HelpBox(
                    "Scene view: kéo sphere trắng/vàng để chỉnh mask. Toolbar phía trên chọn Finger. Delete xóa điểm đang chọn.",
                    MessageType.Info);
            }
        }

        private void DrawBackgroundSection()
        {
            EditorGUILayout.LabelField("Background", EditorStyles.boldLabel);
            var spriteProp = _serializedLayout.FindProperty("backgroundSprite");
            EditorGUILayout.PropertyField(spriteProp, new GUIContent("Sprite"));

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Load v2 Background"))
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
                if (sprite == null)
                {
                    var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(BackgroundPath);
                    if (tex != null)
                    {
                        sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    }
                }

                spriteProp.objectReferenceValue = sprite;
            }

            if (GUILayout.Button("Select PNG"))
            {
                var obj = AssetDatabase.LoadAssetAtPath<Object>(BackgroundPath);
                if (obj != null)
                {
                    Selection.activeObject = obj;
                    EditorGUIUtility.PingObject(obj);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawTerritorySection()
        {
            EditorGUILayout.LabelField("Territories (Vault Masks)", EditorStyles.boldLabel);
            var territoriesProp = _serializedLayout.FindProperty("territories");
            if (territoriesProp == null || !territoriesProp.isArray)
            {
                return;
            }

            var names = new string[territoriesProp.arraySize];
            for (var i = 0; i < territoriesProp.arraySize; i++)
            {
                var entry = territoriesProp.GetArrayElementAtIndex(i);
                var finger = entry.FindPropertyRelative("finger").enumDisplayNames[
                    entry.FindPropertyRelative("finger").enumValueIndex];
                var display = entry.FindPropertyRelative("displayName").stringValue;
                names[i] = string.IsNullOrEmpty(display) ? finger : display;
            }

            _selectedTerritory = Mathf.Clamp(_selectedTerritory, 0, Mathf.Max(0, territoriesProp.arraySize - 1));
            var newSelection = GUILayout.Toolbar(_selectedTerritory, names);
            if (newSelection != _selectedTerritory)
            {
                _selectedTerritory = newSelection;
                CadenceMapMaskEditSession.SelectedTerritory = _selectedTerritory;
                CadenceMapMaskEditSession.SelectedVertex = -1;
                SceneView.RepaintAll();
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            if (territoriesProp.arraySize > 0)
            {
                var entry = territoriesProp.GetArrayElementAtIndex(_selectedTerritory);
                DrawTerritoryEntry(entry, _selectedTerritory);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawTerritoryEntry(SerializedProperty entry, int index)
        {
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("displayName"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("finger"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("unlocked"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("territoryColor"));
            EditorGUILayout.PropertyField(entry.FindPropertyRelative("highlightColor"));

            _showVertices = EditorGUILayout.Foldout(_showVertices, "Mask Vertices (normalized 0–1)", true);
            if (!_showVertices)
            {
                return;
            }

            var vertices = entry.FindPropertyRelative("normalizedVertices");
            EditorGUILayout.HelpBox(
                "X/Y từ góc dưới-trái ảnh. Chỉnh từng điểm để mask khớp vùng Vault trên background.",
                MessageType.Info);

            for (var i = 0; i < vertices.arraySize; i++)
            {
                var v = vertices.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"P{i}", GUILayout.Width(28f));
                v.vector2Value = EditorGUILayout.Vector2Field(GUIContent.none, v.vector2Value);
                if (GUILayout.Button("X", GUILayout.Width(22f)))
                {
                    vertices.DeleteArrayElementAtIndex(i);
                    break;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ Add Vertex"))
            {
                vertices.InsertArrayElementAtIndex(vertices.arraySize);
                vertices.GetArrayElementAtIndex(vertices.arraySize - 1).vector2Value = new Vector2(0.5f, 0.5f);
            }

            if (GUILayout.Button("Reset Finger Defaults"))
            {
                ResetTerritoryToDefault(index);
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawActionButtons()
        {
            EditorGUILayout.LabelField("Scene", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply To Open Scene"))
            {
                ApplyToOpenScene();
            }

            if (GUILayout.Button("Setup Macro Layer"))
            {
                RunMapSceneSetupEditor.SetupCadenceMacroMapLayer();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Asset"))
            {
                SaveLayout();
            }

            if (GUILayout.Button("Reset All Defaults"))
            {
                if (EditorUtility.DisplayDialog("Reset", "Reset toàn bộ territory về mặc định?", "Reset", "Cancel"))
                {
                    _layout.territories = CadenceMapLayoutSO.DefaultTerritories();
                    EditorUtility.SetDirty(_layout);
                    _serializedLayout = new SerializedObject(_layout);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        private void ApplyToOpenScene()
        {
            SaveLayout();

            var macroView = Object.FindAnyObjectByType<CadenceMacroMapView>();
            if (macroView == null)
            {
                EditorUtility.DisplayDialog(
                    "Apply",
                    "Không tìm thấy CadenceMacroMapView. Chạy Run Map → Setup Cadence Macro Layer trước.",
                    "OK");
                return;
            }

            RefreshScenePreview();
            WireCadenceController();

            EditorUtility.SetDirty(macroView);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            Debug.Log("[Fractured Chorus] Cadence layout applied to scene.");
        }

        private void RefreshScenePreview()
        {
            var macroView = Object.FindAnyObjectByType<CadenceMacroMapView>();
            if (macroView == null || _layout == null)
            {
                return;
            }

            macroView.Build(_layout);
            SceneView.RepaintAll();
        }

        private void WireCadenceController()
        {
            var cadence = Object.FindAnyObjectByType<CadenceMapController>();
            if (cadence == null)
            {
                return;
            }

            var so = new SerializedObject(cadence);
            so.FindProperty("layout").objectReferenceValue = _layout;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(cadence);
        }

        private void PrepareEditModeScene()
        {
            var macroRoot = GameObject.Find("MacroMapLayer");
            var innerRoot = GameObject.Find("InnerMapLayer");
            var scroll = GameObject.Find("MapScrollView");
            var legend = GameObject.Find("LegendPanel");

            if (macroRoot != null)
            {
                macroRoot.SetActive(true);
            }

            if (innerRoot != null)
            {
                innerRoot.SetActive(false);
            }

            if (scroll != null && (innerRoot == null || scroll.transform.parent != innerRoot.transform))
            {
                scroll.SetActive(false);
            }

            if (legend != null && (innerRoot == null || legend.transform.parent != innerRoot.transform))
            {
                legend.SetActive(false);
            }

            var macroView = Object.FindAnyObjectByType<CadenceMacroMapView>();
            if (macroView == null)
            {
                RunMapSceneSetupEditor.SetupCadenceMacroMapLayer();
            }
        }

        private void ResetTerritoryToDefault(int index)
        {
            var defaults = CadenceMapLayoutSO.DefaultTerritories();
            if (index < 0 || index >= defaults.Length || index >= _layout.territories.Length)
            {
                return;
            }

            _layout.territories[index] = defaults[index];
            EditorUtility.SetDirty(_layout);
            _serializedLayout = new SerializedObject(_layout);
        }

        private void LoadOrCreateLayout()
        {
            _layout = AssetDatabase.LoadAssetAtPath<CadenceMapLayoutSO>(DefaultLayoutPath);
            if (_layout != null)
            {
                _serializedLayout = new SerializedObject(_layout);
                return;
            }

            if (!AssetDatabase.IsValidFolder("Assets/FracturedChorus/Data/ScriptableObjects/Presets"))
            {
                AssetDatabase.CreateFolder("Assets/FracturedChorus/Data/ScriptableObjects", "Presets");
            }

            _layout = ScriptableObject.CreateInstance<CadenceMapLayoutSO>();
            _layout.territories = CadenceMapLayoutSO.DefaultTerritories();
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(BackgroundPath);
            if (tex != null)
            {
                _layout.backgroundSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }

            AssetDatabase.CreateAsset(_layout, DefaultLayoutPath);
            AssetDatabase.SaveAssets();
            _serializedLayout = new SerializedObject(_layout);
        }

        private void SaveLayout()
        {
            if (_layout == null)
            {
                return;
            }

            EditorUtility.SetDirty(_layout);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
#endif
