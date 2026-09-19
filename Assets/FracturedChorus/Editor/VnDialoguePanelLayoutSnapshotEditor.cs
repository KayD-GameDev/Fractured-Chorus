#if UNITY_EDITOR
using System;
using System.IO;
using FracturedChorus.Narrative.Vn;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Editor
{
    public static class VnDialoguePanelLayoutSnapshotEditor
    {
        public const string OpeningInvestigationLayoutAssetPath =
            "Assets/FracturedChorus/Data/UI/Vn/vn_dialogue_panel_layout_opening_investigation.json";

        private static readonly string[] CapturedChildNames =
        {
            "DialogueBodyBacking",
            "DialogueFrame",
            "Nameplate",
            "DialogueBody"
        };

        [MenuItem("Fractured Chorus/Narrative/Capture Dialogue Panel Layout (Active Scene)")]
        public static void CaptureActiveSceneDialogueLayout()
        {
            var panel = FindDialoguePanelInActiveScene();
            if (panel == null)
            {
                return;
            }

            var scene = panel.gameObject.scene;
            var scenePath = scene.path;
            var file = Capture(panel);
            file.scene = string.IsNullOrEmpty(scenePath) ? scene.name : scenePath;

            var assetPath = ResolveLayoutAssetPath(scenePath);
            WriteLayoutAsset(assetPath, file);
            Debug.Log($"[Fractured Chorus] Captured dialogue panel layout → {assetPath} ({file.nodes.Length} nodes).");
        }

        public static bool ApplyLayout(CanvasGroup dialoguePanel, string layoutAssetPath)
        {
            if (dialoguePanel == null)
            {
                return false;
            }

            var file = LoadLayoutAsset(layoutAssetPath);
            if (file?.nodes == null || file.nodes.Length == 0)
            {
                Debug.LogWarning("[Fractured Chorus] Dialogue layout snapshot missing or empty: " + layoutAssetPath);
                return false;
            }

            for (var i = 0; i < file.nodes.Length; i++)
            {
                var node = file.nodes[i];
                if (node == null || string.IsNullOrEmpty(node.name))
                {
                    continue;
                }

                if (node.name == "DialoguePanel")
                {
                    ApplyRect(dialoguePanel.GetComponent<RectTransform>(), node);
                    continue;
                }

                var child = dialoguePanel.transform.Find(node.name);
                if (child == null)
                {
                    Debug.LogWarning("[Fractured Chorus] Layout snapshot node not found: " + node.name);
                    continue;
                }

                ApplyRect(child as RectTransform, node);
                if (node.siblingIndex >= 0)
                {
                    child.SetSiblingIndex(node.siblingIndex);
                }

                if (node.name == "DialogueFrame")
                {
                    ApplyFrameImage(child.GetComponent<Image>(), node);
                }
            }

            return true;
        }

        public static void ApplyOpeningInvestigationLayout(CanvasGroup dialoguePanel)
        {
            ApplyLayout(dialoguePanel, OpeningInvestigationLayoutAssetPath);
        }

        private static CanvasGroup FindDialoguePanelInActiveScene()
        {
            var runtime = UnityEngine.Object.FindAnyObjectByType<VnRuntimeController>();
            if (runtime?.DialoguePanel != null)
            {
                return runtime.DialoguePanel;
            }

            Debug.LogError("[Fractured Chorus] No DialoguePanel on VnRuntimeController in active scene.");
            return null;
        }

        private static VnDialoguePanelLayoutFile Capture(CanvasGroup dialoguePanel)
        {
            var nodes = new VnDialoguePanelLayoutNode[1 + CapturedChildNames.Length];
            nodes[0] = CaptureRect("DialoguePanel", dialoguePanel.GetComponent<RectTransform>());
            nodes[0].siblingIndex = dialoguePanel.transform.GetSiblingIndex();

            for (var i = 0; i < CapturedChildNames.Length; i++)
            {
                var name = CapturedChildNames[i];
                var t = dialoguePanel.transform.Find(name);
                if (t == null)
                {
                    nodes[i + 1] = new VnDialoguePanelLayoutNode { name = name };
                    continue;
                }

                nodes[i + 1] = CaptureRect(name, t as RectTransform);
                nodes[i + 1].siblingIndex = t.GetSiblingIndex();
                if (name == "DialogueFrame")
                {
                    var image = t.GetComponent<Image>();
                    if (image != null)
                    {
                        nodes[i + 1].imageType = (int)image.type;
                        nodes[i + 1].preserveAspect = image.preserveAspect ? 1 : 0;
                    }
                }
            }

            return new VnDialoguePanelLayoutFile { nodes = nodes };
        }

        private static VnDialoguePanelLayoutNode CaptureRect(string name, RectTransform rect)
        {
            var node = new VnDialoguePanelLayoutNode { name = name };
            if (rect == null)
            {
                return node;
            }

            node.anchorMin = ToV2(rect.anchorMin);
            node.anchorMax = ToV2(rect.anchorMax);
            node.anchoredPosition = ToV2(rect.anchoredPosition);
            node.sizeDelta = ToV2(rect.sizeDelta);
            node.pivot = ToV2(rect.pivot);
            node.imageType = -1;
            node.preserveAspect = -1;
            return node;
        }

        private static void ApplyRect(RectTransform rect, VnDialoguePanelLayoutNode node)
        {
            if (rect == null || node == null)
            {
                return;
            }

            if (node.anchorMin != null)
            {
                rect.anchorMin = new Vector2(node.anchorMin.x, node.anchorMin.y);
            }

            if (node.anchorMax != null)
            {
                rect.anchorMax = new Vector2(node.anchorMax.x, node.anchorMax.y);
            }

            if (node.anchoredPosition != null)
            {
                rect.anchoredPosition = new Vector2(node.anchoredPosition.x, node.anchoredPosition.y);
            }

            if (node.sizeDelta != null)
            {
                rect.sizeDelta = new Vector2(node.sizeDelta.x, node.sizeDelta.y);
            }

            if (node.pivot != null)
            {
                rect.pivot = new Vector2(node.pivot.x, node.pivot.y);
            }
        }

        private static void ApplyFrameImage(Image image, VnDialoguePanelLayoutNode node)
        {
            if (image == null || node == null)
            {
                return;
            }

            if (node.imageType >= 0)
            {
                image.type = (Image.Type)node.imageType;
            }

            if (node.preserveAspect >= 0)
            {
                image.preserveAspect = node.preserveAspect != 0;
            }
        }

        private static VnDialoguePanelLayoutFile LoadLayoutAsset(string assetPath)
        {
            var fullPath = Path.GetFullPath(assetPath);
            if (!File.Exists(fullPath))
            {
                return null;
            }

            return JsonUtility.FromJson<VnDialoguePanelLayoutFile>(File.ReadAllText(fullPath));
        }

        private static void WriteLayoutAsset(string assetPath, VnDialoguePanelLayoutFile file)
        {
            var fullPath = Path.GetFullPath(assetPath);
            var dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(fullPath, JsonUtility.ToJson(file, prettyPrint: true));
            AssetDatabase.Refresh();
        }

        private static string ResolveLayoutAssetPath(string scenePath)
        {
            if (scenePath != null && scenePath.Contains("OpeningInvestigation"))
            {
                return OpeningInvestigationLayoutAssetPath;
            }

            if (scenePath != null && scenePath.Contains("FlowerShopWork"))
            {
                return "Assets/FracturedChorus/Data/UI/Vn/vn_dialogue_panel_layout_flower_shop_work.json";
            }

            if (string.IsNullOrEmpty(scenePath))
            {
                return OpeningInvestigationLayoutAssetPath;
            }

            var fileName = Path.GetFileNameWithoutExtension(scenePath);
            return $"Assets/FracturedChorus/Data/UI/Vn/vn_dialogue_panel_layout_{fileName.ToLowerInvariant()}.json";
        }

        private static VnDialoguePanelLayoutV2 ToV2(Vector2 v)
        {
            return new VnDialoguePanelLayoutV2 { x = v.x, y = v.y };
        }

        [Serializable]
        private sealed class VnDialoguePanelLayoutFile
        {
            public string scene;
            public VnDialoguePanelLayoutNode[] nodes;
        }

        [Serializable]
        private sealed class VnDialoguePanelLayoutNode
        {
            public string name;
            public int siblingIndex = -1;
            public VnDialoguePanelLayoutV2 anchorMin;
            public VnDialoguePanelLayoutV2 anchorMax;
            public VnDialoguePanelLayoutV2 anchoredPosition;
            public VnDialoguePanelLayoutV2 sizeDelta;
            public VnDialoguePanelLayoutV2 pivot;
            public int imageType = -1;
            public int preserveAspect = -1;
        }

        [Serializable]
        private sealed class VnDialoguePanelLayoutV2
        {
            public float x;
            public float y;
        }
    }
}
#endif
