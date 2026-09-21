#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FracturedChorus.Editor
{
    public static class OffBeatSyncPodLayoutSnapshotEditor
    {
        public const string SnapshotAssetPath =
            "Assets/FracturedChorus/Data/UI/OffBeat/offbeat_syncpod_layout_snapshot.json";

        private static readonly string[] CapturePaths =
        {
            "OffBeatArchiveOverlay/ArchivePanel/PlayerRoot/VolumeArcRoot",
            "OffBeatArchiveOverlay/ArchivePanel/PlayerRoot/DiscFace",
            "OffBeatArchiveOverlay/ArchivePanel/PlayerRoot/DiscFace/CoverImage",
            "OffBeatArchiveOverlay/ArchivePanel/PlayerRoot/DiscFace/SongTitle",
            "OffBeatArchiveOverlay/ArchivePanel/PlayerRoot/DiscFace/SongTitle/Label",
            "OffBeatArchiveOverlay/ArchivePanel/PlayerRoot/DiscFace/Waveform",
            "OffBeatArchiveOverlay/ArchivePanel/PlayerRoot/DiscFace/Controls"
        };

        private const string VolumeArcRelativePath =
            "OffBeatArchiveOverlay/ArchivePanel/PlayerRoot/VolumeArcRoot";

        private const string ControlsRelativePath =
            "OffBeatArchiveOverlay/ArchivePanel/PlayerRoot/DiscFace/Controls";

        [MenuItem("Fractured Chorus/Menu/Save Off-Beat SyncPod Layout Snapshot")]
        public static void SaveSyncPodLayoutSnapshotMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Save SyncPod layout", "Thoát Play Mode trước.", "OK");
                return;
            }

            if (!TryCaptureFromScene(out var file))
            {
                EditorUtility.DisplayDialog(
                    "Save SyncPod layout",
                    "Không tìm thấy OffBeatArchiveOverlay / VolumeArcRoot trong scene đang mở.",
                    "OK");
                return;
            }

            WriteSnapshot(file);
            Debug.Log("[Fractured Chorus] Saved Off-Beat SyncPod layout → " + SnapshotAssetPath);
        }

        [MenuItem("Fractured Chorus/Menu/Apply Off-Beat SyncPod Layout (Active Scene)")]
        public static void ApplySyncPodLayoutSnapshotMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Apply SyncPod layout", "Thoát Play Mode trước.", "OK");
                return;
            }

            if (!TryApplyAllFromSnapshot(out var missing))
            {
                EditorUtility.DisplayDialog(
                    "Apply SyncPod layout",
                    "Thiếu node trong scene:\n" + missing,
                    "OK");
                return;
            }

            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[Fractured Chorus] Applied SyncPod layout from snapshot (Ctrl+S to persist scene).");
        }

        public static void TryApplyVolumeArcRoot(RectTransform volRoot)
        {
            if (volRoot == null)
            {
                return;
            }

            var node = LoadNode(VolumeArcRelativePath);
            if (node == null)
            {
                return;
            }

            ApplyNode(volRoot, node);
        }

        public static void TryApplyControls(RectTransform controls)
        {
            if (controls == null)
            {
                return;
            }

            var node = LoadNode(ControlsRelativePath);
            if (node == null)
            {
                return;
            }

            ApplyNode(controls, node);
        }

        public static void TryApplySyncPodLayoutFromSnapshot()
        {
            TryApplyAllFromSnapshot(out _);
        }

        public static bool TryApplyAllFromSnapshot(out string missingPath)
        {
            missingPath = null;
            var file = LoadFile();
            if (file?.nodes == null || file.nodes.Length == 0)
            {
                missingPath = SnapshotAssetPath;
                return false;
            }

            foreach (var node in file.nodes)
            {
                if (string.IsNullOrEmpty(node.path))
                {
                    continue;
                }

                var tf = GameObject.Find("MainMenuCanvas")?.transform.Find(node.path);
                if (tf == null)
                {
                    missingPath = node.path;
                    return false;
                }

                ApplyNode(tf as RectTransform, node);
            }

            return true;
        }

        private static bool TryCaptureFromScene(out OffBeatSyncPodLayoutFile file)
        {
            file = new OffBeatSyncPodLayoutFile
            {
                note =
                    "Layout SoT = MainMenuStartGame.unity Hierarchy. Rebuild SyncPod applies this only when creating VolumeArcRoot/Controls. Save via menu after manual tune.",
                nodes = Array.Empty<OffBeatSyncPodLayoutNode>()
            };

            var canvas = GameObject.Find("MainMenuCanvas");
            if (canvas == null)
            {
                return false;
            }

            var nodes = new System.Collections.Generic.List<OffBeatSyncPodLayoutNode>();
            foreach (var path in CapturePaths)
            {
                var rect = canvas.transform.Find(path) as RectTransform;
                if (rect == null)
                {
                    continue;
                }

                nodes.Add(CaptureNode(path, rect));
            }

            if (nodes.Count == 0)
            {
                return false;
            }

            file.nodes = nodes.ToArray();
            return true;
        }

        private static OffBeatSyncPodLayoutNode CaptureNode(string path, RectTransform rect)
        {
            return new OffBeatSyncPodLayoutNode
            {
                path = path,
                anchorMin = ToVec(rect.anchorMin),
                anchorMax = ToVec(rect.anchorMax),
                pivot = ToVec(rect.pivot),
                anchoredPosition = ToVec(rect.anchoredPosition),
                sizeDelta = ToVec(rect.sizeDelta),
                offsetMin = ToVec(rect.offsetMin),
                offsetMax = ToVec(rect.offsetMax),
                localEulerZ = rect.localEulerAngles.z
            };
        }

        private static void ApplyNode(RectTransform rect, OffBeatSyncPodLayoutNode node)
        {
            if (rect == null || node == null)
            {
                return;
            }

            rect.anchorMin = ToVec(node.anchorMin);
            rect.anchorMax = ToVec(node.anchorMax);
            rect.pivot = ToVec(node.pivot);
            rect.anchoredPosition = ToVec(node.anchoredPosition);
            rect.sizeDelta = ToVec(node.sizeDelta);
            rect.offsetMin = ToVec(node.offsetMin);
            rect.offsetMax = ToVec(node.offsetMax);
            rect.localEulerAngles = new Vector3(0f, 0f, node.localEulerZ);
        }

        private static OffBeatSyncPodLayoutNode LoadNode(string path)
        {
            var file = LoadFile();
            if (file?.nodes == null)
            {
                return null;
            }

            foreach (var node in file.nodes)
            {
                if (node.path == path)
                {
                    return node;
                }
            }

            return null;
        }

        private static OffBeatSyncPodLayoutFile LoadFile()
        {
            var fullPath = Path.GetFullPath(SnapshotAssetPath);
            if (!File.Exists(fullPath))
            {
                return null;
            }

            return JsonUtility.FromJson<OffBeatSyncPodLayoutFile>(File.ReadAllText(fullPath));
        }

        private static void WriteSnapshot(OffBeatSyncPodLayoutFile file)
        {
            var fullPath = Path.GetFullPath(SnapshotAssetPath);
            var dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(fullPath, JsonUtility.ToJson(file, true));
            AssetDatabase.Refresh();
        }

        private static SyncPodVec2 ToVec(Vector2 v) => new SyncPodVec2 { x = v.x, y = v.y };

        private static Vector2 ToVec(SyncPodVec2 v) => new Vector2(v.x, v.y);

        [Serializable]
        private sealed class OffBeatSyncPodLayoutFile
        {
            public string note;
            public OffBeatSyncPodLayoutNode[] nodes;
        }

        [Serializable]
        private sealed class OffBeatSyncPodLayoutNode
        {
            public string path;
            public SyncPodVec2 anchorMin;
            public SyncPodVec2 anchorMax;
            public SyncPodVec2 pivot;
            public SyncPodVec2 anchoredPosition;
            public SyncPodVec2 sizeDelta;
            public SyncPodVec2 offsetMin;
            public SyncPodVec2 offsetMax;
            public float localEulerZ;
        }

        [Serializable]
        private sealed class SyncPodVec2
        {
            public float x;
            public float y;
        }
    }
}
#endif
