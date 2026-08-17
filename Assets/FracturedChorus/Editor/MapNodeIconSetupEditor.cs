#if UNITY_EDITOR
using FracturedChorus.Data;
using FracturedChorus.RunMap;
using FracturedChorus.RunMap.Core;
using FracturedChorus.RunMap.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace FracturedChorus.Editor
{
    public static class MapNodeIconSetupEditor
    {
        private const string NodesRoot = "Assets/FracturedChorus/Art/UI/RunMap/Nodes/";
        private const string IconSetPath = "Assets/FracturedChorus/Data/ScriptableObjects/Presets/MapNodeIconSet_Default.asset";
        private const string ScenePath = "Assets/FracturedChorus/Scenes/RunMapPrototype.unity";

        private const string RenAvatarPath =
            "Assets/FracturedChorus/Art/UI/Combat/Timeline/LeftRail/Avatars/ren_chibi_avatar_v1.png";

        private const string MapBackgroundVideoPath =
            "Assets/FracturedChorus/Art/UI/RunMap/Backgrounds/runmap_stage_background_anim_v1.mp4";

        private const string MapBackgroundSpritePath =
            "Assets/FracturedChorus/Art/UI/RunMap/Backgrounds/runmap_stage_background_v1.png";

        public static void AssignRenMarkerToMapView(RunMapUIView mapView)
        {
            if (mapView == null)
            {
                return;
            }

#if UNITY_EDITOR
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(RenAvatarPath);
            if (sprite == null)
            {
                return;
            }

            var so = new SerializedObject(mapView);
            var prop = so.FindProperty("playerMarkerSprite");
            if (prop != null)
            {
                prop.objectReferenceValue = sprite;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(mapView);
            }
#endif
        }

        [MenuItem("Fractured Chorus/Run Map/Wire Scene Edit Chrome", false, 37)]
        public static void WireSceneEditChrome()
        {
            var iconSet = EnsureIconSetAsset();
            var sidebar = EnsureNodeInfoSidebar(true);
            WireRunMapControllerPanel(sidebar);

            var cadence = Object.FindAnyObjectByType<CadenceMapController>();
            if (cadence != null)
            {
                cadence.WireSceneEditChrome();
                var so = new SerializedObject(cadence);
                so.FindProperty("nodeIconSet").objectReferenceValue = iconSet;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(cadence);
            }

            AssignToOpenScene(iconSet);
            TreasureRoomOverlaySetupEditor.SetupTreasureRoomOverlay();
            EventRoomOverlaySetupEditor.SetupEventRoomOverlay();
            CampRoomOverlaySetupEditor.SetupCampRoomOverlay();
            EditorSceneManager.MarkAllScenesDirty();
            Debug.Log("[Fractured Chorus] Wired scene edit chrome — NodeInfoSidebar visible in Map Nodes preview.");
        }

        [MenuItem("Fractured Chorus/Run Map/Upgrade Map Chrome", false, 36)]
        public static void UpgradeRunMapChrome()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas != null)
            {
                var stale = canvas.transform.Find("NodeInfoSidebar");
                if (stale != null)
                {
                    Object.DestroyImmediate(stale.gameObject);
                }
            }

            var scroll = GameObject.Find("MapScrollView");
            if (scroll != null)
            {
                var scrollRect = scroll.GetComponent<ScrollRect>();
                var parent = scrollRect?.viewport != null ? scrollRect.viewport : scroll.transform;
                RunMapNodeInfoPanelBuilder.EnsureSidebar(parent, showEditPreview: true);

                var content = scrollRect != null ? scrollRect.content : scroll.transform;
                var legacyBg = scroll.transform.Find("BackgroundLayer");
                if (legacyBg != null && legacyBg.parent != content)
                {
                    legacyBg.SetParent(content, false);
                }

                var bg = content.Find("BackgroundLayer");
                if (bg == null)
                {
                    var go = new GameObject("BackgroundLayer", typeof(RectTransform), typeof(RunMapBackgroundView));
                    go.transform.SetParent(content, false);
                    go.transform.SetAsFirstSibling();
                    go.AddComponent<RunMapBackgroundView>();
                }

                WireMapBackgroundVideo();
            }

            RunMapPlayerMarkerSetupEditor.WireSceneMarkers();
            RunMapPlayerMarkerSetupEditor.WireMapLayout();
            WireSceneEditChrome();

            foreach (var mapView in Object.FindObjectsByType<RunMapUIView>(FindObjectsInactive.Include))
            {
                mapView.EnsureEditModePlayerMarker();
            }

            EditorSceneManager.MarkAllScenesDirty();
            Debug.Log("[Fractured Chorus] Upgraded map chrome — sidebar + icon strip + background + layout + Ren marker.");
        }

        private static RunMapNodeInfoPanel EnsureNodeInfoSidebar(bool showEditPreview)
        {
            var scroll = GameObject.Find("MapScrollView");
            if (scroll == null)
            {
                return null;
            }

            var scrollRect = scroll.GetComponent<ScrollRect>();
            var parent = scrollRect?.viewport != null ? scrollRect.viewport : scroll.transform;
            return RunMapNodeInfoPanelBuilder.EnsureSidebar(parent, showEditPreview);
        }

        private static void WireRunMapControllerPanel(RunMapNodeInfoPanel panel)
        {
            if (panel == null)
            {
                return;
            }

            var controller = Object.FindAnyObjectByType<RunMapController>();
            if (controller == null)
            {
                return;
            }

            var so = new SerializedObject(controller);
            var prop = so.FindProperty("nodeInfoPanel");
            if (prop != null)
            {
                prop.objectReferenceValue = panel;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(controller);
            }
        }

        public static void WireMapBackgroundVideo()
        {
            var video = AssetDatabase.LoadAssetAtPath<VideoClip>(MapBackgroundVideoPath);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(MapBackgroundSpritePath);
            if (video == null)
            {
                Debug.LogWarning($"[Fractured Chorus] Missing map BG video at {MapBackgroundVideoPath}");
            }

            foreach (var bgView in Object.FindObjectsByType<RunMapBackgroundView>(FindObjectsInactive.Include))
            {
                var so = new SerializedObject(bgView);
                so.FindProperty("backgroundVideo").objectReferenceValue = video;
                so.FindProperty("backgroundSprite").objectReferenceValue = sprite;
                so.FindProperty("preferVideo").boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(bgView);
            }

            foreach (var mapView in Object.FindObjectsByType<RunMapUIView>(FindObjectsInactive.Include))
            {
                var so = new SerializedObject(mapView);
                so.FindProperty("mapBackgroundVideo").objectReferenceValue = video;
                so.FindProperty("mapBackgroundSprite").objectReferenceValue = sprite;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(mapView);
            }
        }

        [MenuItem("Fractured Chorus/Run Map/Wire Node Icons", false, 35)]
        public static void WireNodeIcons()
        {
            EnsureSpriteImports();
            var iconSet = EnsureIconSetAsset();
            AssignToOpenScene(iconSet);
            AssignRenMarkerToMapView(Object.FindAnyObjectByType<RunMapUIView>());
            MapNodeTemplateSetEditor.EnsureAssets();
            EditorSceneManager.MarkAllScenesDirty();
            AssetDatabase.SaveAssets();
            Debug.Log($"[Fractured Chorus] Wired MapNodeIconSet → {IconSetPath}");
        }

        public static MapNodeIconSetSO EnsureIconSetAsset()
        {
            EnsureSpriteImports();
            var set = AssetDatabase.LoadAssetAtPath<MapNodeIconSetSO>(IconSetPath);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<MapNodeIconSetSO>();
                AssetDatabase.CreateAsset(set, IconSetPath);
            }

            set.EditorAssign(
                LoadSprite("runmap_node_battle_v1.png"),
                LoadSprite("runmap_node_elite_v1.png"),
                LoadSprite("runmap_node_treasure_v1.png"),
                LoadSprite("runmap_node_event_v1.png"),
                LoadSprite("runmap_node_boss_floor_i_v1.png"),
                LoadSprite("runmap_node_boss_floor_ii_v1.png"),
                LoadSprite("runmap_node_boss_final_v1.png"),
                LoadSprite("runmap_node_camp_v1.png"),
                LoadSprite("runmap_node_relay_v1.png"),
                LoadStartSprite());

            if (set.Resolve(MapNodeType.Start, false, PinkySectorId.Pulse) == null)
            {
                Debug.LogWarning("[Fractured Chorus] Start node sprite missing — kiểm tra runmap_node_start_v1.png import.");
            }

            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();
            return set;
        }

        private static void AssignToOpenScene(MapNodeIconSetSO iconSet)
        {
            var mapView = Object.FindAnyObjectByType<RunMapUIView>();
            if (mapView != null)
            {
                var so = new SerializedObject(mapView);
                var prop = so.FindProperty("iconSet");
                if (prop != null)
                {
                    prop.objectReferenceValue = iconSet;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(mapView);
                }
            }

            var legend = Object.FindAnyObjectByType<RunMapLegendPanelView>();
            if (legend != null)
            {
                var so = new SerializedObject(legend);
                var prop = so.FindProperty("iconSet");
                if (prop != null)
                {
                    prop.objectReferenceValue = iconSet;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(legend);
                    legend.Apply();
                }
            }

            if (mapView == null && legend == null)
            {
                var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                mapView = Object.FindAnyObjectByType<RunMapUIView>();
                legend = Object.FindAnyObjectByType<RunMapLegendPanelView>();
                if (mapView != null)
                {
                    var so = new SerializedObject(mapView);
                    so.FindProperty("iconSet").objectReferenceValue = iconSet;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(mapView);
                }

                if (legend != null)
                {
                    var so = new SerializedObject(legend);
                    so.FindProperty("iconSet").objectReferenceValue = iconSet;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(legend);
                    legend.Apply();
                }

                EditorSceneManager.SaveScene(scene);
            }
        }

        private static void EnsureSpriteImports()
        {
            string[] files =
            {
                "runmap_node_battle_v1.png",
                "runmap_node_elite_v1.png",
                "runmap_node_treasure_v1.png",
                "runmap_node_event_v1.png",
                "runmap_node_camp_v1.png",
                "runmap_node_relay_v1.png",
                "runmap_node_start_v1.png",
                "runmap_node_boss_floor_i_v1.png",
                "runmap_node_boss_floor_ii_v1.png",
                "runmap_node_boss_final_v1.png"
            };

            foreach (var file in files)
            {
                var path = NodesRoot + file;
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                var dirty = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    dirty = true;
                }

                if (importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                    dirty = true;
                }

                if (importer.mipmapEnabled)
                {
                    importer.mipmapEnabled = false;
                    dirty = true;
                }

                if (!importer.alphaIsTransparency)
                {
                    importer.alphaIsTransparency = true;
                    dirty = true;
                }

                if (importer.maxTextureSize < 1024)
                {
                    importer.maxTextureSize = 1024;
                    dirty = true;
                }

                if (dirty)
                {
                    importer.SaveAndReimport();
                }
            }
        }

        private static Sprite LoadSprite(string fileName)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(NodesRoot + fileName);
        }

        private static Sprite LoadStartSprite()
        {
            var path = NodesRoot + "runmap_node_start_v1.png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
            {
                return sprite;
            }

            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var asset in assets)
            {
                if (asset is Sprite loaded)
                {
                    return loaded;
                }
            }

            return null;
        }
    }
}
#endif
