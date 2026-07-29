#if UNITY_EDITOR
using FracturedChorus.Narrative;
using FracturedChorus.Narrative.Vn;
using FracturedChorus.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Editor
{
    public static class VnSceneUiSetupEditor
    {
        public const string DialogueFramePath =
            "Assets/FracturedChorus/Art/UI/Narrative/DialogueBox_Frame_LightBlueHolo_v1.png";
        private const string TownMapUiRoot = "Assets/FracturedChorus/Art/UI/TownMap/";

        [MenuItem("Fractured Chorus/Narrative/Heal Active Scene VN Layout")]
        public static void HealActiveSceneVnLayout()
        {
            var runtime = Object.FindAnyObjectByType<VnRuntimeController>();
            if (runtime == null)
            {
                Debug.LogError("[Fractured Chorus] No VnRuntimeController in active scene.");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "Reset VN layout",
                    "Ghi đè vị trí/kích thước UI về chuẩn OpeningInvestigation. Chỉ dùng khi cần reset — không chạy sau khi đã chỉnh tay.",
                    "Reset layout",
                    "Cancel"))
            {
                return;
            }

            ApplyStandardVnSceneLayout(runtime);
            EditorSceneManager.MarkSceneDirty(runtime.gameObject.scene);
            Debug.Log($"[Fractured Chorus] Applied standard VN layout to {runtime.gameObject.scene.name}.");
        }

        [MenuItem("Fractured Chorus/Narrative/Heal FlowerShopWork VN Layout")]
        public static void HealFlowerShopVnLayout()
        {
            const string scenePath = "Assets/FracturedChorus/Scenes/FlowerShopWork.unity";
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var runtime = Object.FindAnyObjectByType<VnRuntimeController>();
            if (runtime == null)
            {
                Debug.LogError("[Fractured Chorus] VnRuntimeController missing in FlowerShopWork.");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "Reset FlowerShop VN layout",
                    "Ghi đè vị trí/kích thước UI về chuẩn OpeningInvestigation. Chỉ dùng khi cần reset — không chạy sau khi đã chỉnh tay.",
                    "Reset layout",
                    "Cancel"))
            {
                return;
            }

            ApplyStandardVnSceneLayout(runtime);
            SetSerializedField(runtime, "openingDateDisplay", "01/09");
            SetSerializedField(runtime, "openingPhaseDisplay", "After School");
            EditorSceneManager.MarkSceneDirty(runtime.gameObject.scene);
            EditorSceneManager.SaveScene(runtime.gameObject.scene);
            Debug.Log("[Fractured Chorus] Healed FlowerShopWork VN layout (synced with OpeningInvestigation).");
        }

        public static void ApplyStandardVnSceneLayout(VnRuntimeController runtime)
        {
            if (runtime == null)
            {
                return;
            }

            var dialoguePanel = runtime.DialoguePanel;
            var canvas = dialoguePanel != null ? dialoguePanel.transform.parent : null;
            if (canvas == null)
            {
                canvas = Object.FindAnyObjectByType<Canvas>()?.transform;
            }

            ApplyDialoguePanelLayout(dialoguePanel);
            ApplyTextCardLayout(runtime);
            ApplyChoicePanelLayout(canvas);
            ApplyCanvasSiblingOrder(runtime, canvas);
            ApplyPortraitLayoutInScene();
            ApplyReadableTextInScene(runtime);

            var dateHud = EnsureStoryDateHud(canvas);
            if (dateHud != null)
            {
                SetSerializedField(runtime, "dateHud", dateHud);
            }
        }

        public static VnStoryDateHud EnsureStoryDateHud(Transform canvas)
        {
            if (canvas == null)
            {
                return null;
            }

            var existing = canvas.Find("StoryDateHud")?.GetComponent<VnStoryDateHud>();
            GameObject go;
            VnStoryDateHud hud;
            if (existing != null)
            {
                go = existing.gameObject;
                hud = existing;
            }
            else
            {
                go = new GameObject("StoryDateHud", typeof(RectTransform));
                go.transform.SetParent(canvas, false);
                hud = go.AddComponent<VnStoryDateHud>();
            }

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = VnDialoguePanelLayout.DateHudAnchorMin;
            rect.anchorMax = VnDialoguePanelLayout.DateHudAnchorMax;
            rect.pivot = VnDialoguePanelLayout.DateHudPivot;
            rect.offsetMin = VnDialoguePanelLayout.DateHudOffsetMin;
            rect.offsetMax = VnDialoguePanelLayout.DateHudOffsetMax;

            var banner = GetOrAddComponent<Image>(go);
            banner.sprite = LoadSprite(TownMapUiRoot + "townmap_slash_banner.png");
            banner.type = Image.Type.Sliced;
            banner.color = Color.white;
            banner.raycastTarget = false;

            var date = GetOrCreateText(go.transform, "DateLabel", "17/08", VnDialoguePanelLayout.DateLabelFontSize, TextAnchor.MiddleRight);
            PlaceRect(
                date.gameObject,
                VnDialoguePanelLayout.DateLabelAnchorMin,
                VnDialoguePanelLayout.DateLabelAnchorMax,
                VnDialoguePanelLayout.DateLabelOffsetMin,
                VnDialoguePanelLayout.DateLabelOffsetMax);
            date.fontStyle = FontStyle.Normal;
            date.resizeTextForBestFit = false;
            date.horizontalOverflow = HorizontalWrapMode.Overflow;
            date.verticalOverflow = VerticalWrapMode.Overflow;
            date.raycastTarget = false;
            UiFontCatalog.Apply(date, UiFontRole.Display, VnDialoguePanelLayout.DateLabelFontSize);

            var phaseIcon = GetOrCreateImage(go.transform, "PhaseIcon", LoadSprite(TownMapUiRoot + "townmap_icon_moon.png"), Color.white);
            PlaceRect(
                phaseIcon.gameObject,
                VnDialoguePanelLayout.PhaseIconAnchorMin,
                VnDialoguePanelLayout.PhaseIconAnchorMax,
                VnDialoguePanelLayout.PhaseIconOffsetMin,
                VnDialoguePanelLayout.PhaseIconOffsetMax);
            phaseIcon.preserveAspect = true;
            phaseIcon.raycastTarget = false;

            var phase = GetOrCreateText(go.transform, "PhaseLabel", "Late Night", VnDialoguePanelLayout.PhaseLabelFontSize, TextAnchor.MiddleRight);
            PlaceRect(
                phase.gameObject,
                VnDialoguePanelLayout.PhaseLabelAnchorMin,
                VnDialoguePanelLayout.PhaseLabelAnchorMax,
                VnDialoguePanelLayout.PhaseLabelOffsetMin,
                VnDialoguePanelLayout.PhaseLabelOffsetMax);
            phase.color = VnDialoguePanelLayout.PhaseLabelColor;
            phase.verticalOverflow = VerticalWrapMode.Overflow;
            phase.raycastTarget = false;
            UiFontCatalog.Apply(phase, UiFontRole.DisplaySecondary, VnDialoguePanelLayout.PhaseLabelFontSize);

            SetSerializedField(hud, "bannerImage", banner);
            SetSerializedField(hud, "dateLabel", date);
            SetSerializedField(hud, "phaseLabel", phase);
            SetSerializedField(hud, "phaseIcon", phaseIcon);
            SetSerializedField(hud, "sunSprite", LoadSprite(TownMapUiRoot + "townmap_icon_sun.png"));
            SetSerializedField(hud, "moonSprite", LoadSprite(TownMapUiRoot + "townmap_icon_moon.png"));
            SetSerializedField(hud, "dawnSprite", LoadSprite(TownMapUiRoot + "townmap_icon_dawn.png"));

            var fade = canvas.Find("FadeOverlay");
            if (fade != null)
            {
                go.transform.SetSiblingIndex(fade.GetSiblingIndex());
            }

            go.SetActive(false);
            EditorUtility.SetDirty(hud);
            return hud;
        }

        private static void ApplyDialoguePanelLayout(CanvasGroup dialoguePanel)
        {
            if (dialoguePanel == null)
            {
                return;
            }

            Stretch(
                dialoguePanel.GetComponent<RectTransform>(),
                VnDialoguePanelLayout.DialoguePanelAnchorMin,
                VnDialoguePanelLayout.DialoguePanelAnchorMax);

            var frame = dialoguePanel.transform.Find("DialogueFrame")?.GetComponent<Image>();
            if (frame != null)
            {
                frame.gameObject.SetActive(true);
                frame.sprite = LoadSprite(DialogueFramePath);
                frame.type = Image.Type.Sliced;
                frame.preserveAspect = false;
                frame.fillCenter = true;
                frame.color = Color.white;
                frame.raycastTarget = false;
            }

            EnsureBodyBacking(dialoguePanel.transform);

            var nameplate = dialoguePanel.transform.Find("Nameplate")?.GetComponent<Text>();
            if (nameplate != null)
            {
                Stretch(nameplate.rectTransform, VnDialoguePanelLayout.NameplateAnchorMin, VnDialoguePanelLayout.NameplateAnchorMax);
                VnUiFont.ApplyReadableNameplate(nameplate);
            }

            var body = dialoguePanel.transform.Find("DialogueBody")?.GetComponent<Text>();
            if (body != null)
            {
                Stretch(body.rectTransform, VnDialoguePanelLayout.BodyAnchorMin, VnDialoguePanelLayout.BodyAnchorMax);
                VnUiFont.ApplyReadableBody(body);
            }
        }

        private static void ApplyTextCardLayout(VnRuntimeController runtime)
        {
            var cardPanel = runtime.TextCardPanel;
            if (cardPanel == null)
            {
                return;
            }

            var dim = cardPanel.transform.Find("TextCardDim")?.GetComponent<Image>();
            if (dim != null)
            {
                dim.color = VnDialoguePanelLayout.TextCardDimColor;
            }

            var body = runtime.TextCardBody;
            if (body != null)
            {
                Stretch(body.rectTransform, VnDialoguePanelLayout.TextCardBodyAnchorMin, VnDialoguePanelLayout.TextCardBodyAnchorMax);
                VnUiFont.ApplyReadableBody(body, VnDialoguePanelLayout.TextCardFontSize);
                body.alignment = TextAnchor.MiddleCenter;
            }
        }

        private static void ApplyChoicePanelLayout(Transform canvas)
        {
            if (canvas == null)
            {
                return;
            }

            var choice = canvas.Find("ChoicePanel") as RectTransform;
            if (choice != null)
            {
                Stretch(choice, VnDialoguePanelLayout.ChoicePanelAnchorMin, VnDialoguePanelLayout.ChoicePanelAnchorMax);
            }
        }

        private static void ApplyCanvasSiblingOrder(VnRuntimeController runtime, Transform canvas)
        {
            if (canvas == null || runtime.DialoguePanel == null)
            {
                return;
            }

            var panel = runtime.DialoguePanel.transform;
            var fade = canvas.Find("FadeOverlay");
            var convenience = canvas.Find("VnConvenienceRoot");
            var insertIndex = canvas.childCount;
            if (fade != null)
            {
                insertIndex = fade.GetSiblingIndex();
            }
            else if (convenience != null)
            {
                insertIndex = convenience.GetSiblingIndex();
            }

            panel.SetSiblingIndex(Mathf.Max(0, insertIndex - 1));

            var textCard = runtime.TextCardPanel;
            if (textCard != null)
            {
                textCard.transform.SetSiblingIndex(panel.GetSiblingIndex() + 1);
            }

            var choice = canvas.Find("ChoicePanel");
            if (choice != null)
            {
                choice.SetSiblingIndex(panel.GetSiblingIndex() + 2);
            }
        }

        public static void ApplyPortraitLayoutInScene()
        {
            foreach (var view in Object.FindObjectsByType<VnDialoguePortraitView>(FindObjectsInactive.Include))
            {
                view.ApplyStandardLayout();
                EditorUtility.SetDirty(view);
            }
        }

        private static void ApplyReadableTextInScene(VnRuntimeController runtime)
        {
            VnUiFont.ApplyReadableNameplate(runtime.NameplateText);
            var typewriter = runtime.GetComponentInChildren<PrologueTypewriterView>(true);
            var body = typewriter != null ? typewriter.BodyText : null;
            VnUiFont.ApplyReadableBody(body);
            VnUiFont.ApplyReadableBody(runtime.TextCardBody, VnDialoguePanelLayout.TextCardFontSize);
            runtime.DateHud?.ApplyFonts();
        }

        private static void EnsureBodyBacking(Transform dialoguePanel)
        {
            var existing = dialoguePanel.Find("DialogueBodyBacking");
            if (existing == null)
            {
                var go = new GameObject("DialogueBodyBacking", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(dialoguePanel, false);
                existing = go.transform;
            }

            existing.SetAsFirstSibling();
            var image = existing.GetComponent<Image>() ?? existing.gameObject.AddComponent<Image>();
            image.color = VnDialoguePanelLayout.BodyBackingColor;
            image.raycastTarget = false;
            Stretch(existing.GetComponent<RectTransform>(), VnDialoguePanelLayout.BodyBackingAnchorMin, VnDialoguePanelLayout.BodyBackingAnchorMax);

            var frame = dialoguePanel.Find("DialogueFrame");
            if (frame != null)
            {
                frame.SetSiblingIndex(existing.GetSiblingIndex() + 1);
            }
        }

        private static Text GetOrCreateText(Transform parent, string name, string content, int fontSize, TextAnchor anchor)
        {
            var existing = parent.Find(name)?.GetComponent<Text>();
            if (existing != null)
            {
                existing.text = content;
                return existing;
            }

            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.text = content;
            text.alignment = anchor;
            text.color = Color.white;
            VnUiFont.Apply(text, fontSize, FontStyle.Normal);
            return text;
        }

        private static Image GetOrCreateImage(Transform parent, string name, Sprite sprite, Color color)
        {
            var existing = parent.Find(name)?.GetComponent<Image>();
            if (existing != null)
            {
                existing.sprite = sprite;
                existing.color = color;
                return existing;
            }

            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            return image;
        }

        private static T GetOrAddComponent<T>(GameObject go) where T : Component
        {
            return go.GetComponent<T>() ?? go.AddComponent<T>();
        }

        private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        private static void PlaceRect(
            GameObject go,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }

        private static Sprite LoadSprite(string assetPath)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite != null)
            {
                return sprite;
            }

            var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            foreach (var asset in assets)
            {
                if (asset is Sprite found)
                {
                    return found;
                }
            }

            return null;
        }

        private static void SetSerializedField(Object target, string fieldName, object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[Fractured Chorus] Missing field {fieldName} on {target.name}");
                return;
            }

            switch (value)
            {
                case null:
                    prop.objectReferenceValue = null;
                    break;
                case Object obj:
                    prop.objectReferenceValue = obj;
                    break;
                case bool b:
                    prop.boolValue = b;
                    break;
                case string s:
                    prop.stringValue = s;
                    break;
                case float f:
                    prop.floatValue = f;
                    break;
                case int i:
                    prop.intValue = i;
                    break;
                default:
                    Debug.LogWarning($"[Fractured Chorus] Unsupported serialize type for {fieldName}");
                    break;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
#endif
