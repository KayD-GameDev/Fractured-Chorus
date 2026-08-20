#if UNITY_EDITOR
using FracturedChorus.Menu;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Editor
{
    public static class ConfigUiKitApply
    {
        private const string KitDir = "Assets/FracturedChorus/Art/UI/ConfigMenu/Kit/";
        private const string PanelPath = KitDir + "ui_config_panel_v1.png";
        private const string SliderTrackPath = KitDir + "ui_config_slider_track_v1.png";
        private const string SliderFillPath = KitDir + "ui_config_slider_fill_v1.png";
        private const string SliderHandlePath = KitDir + "ui_config_slider_handle_v1.png";
        private const string ToggleOnPath = KitDir + "ui_config_toggle_on_v1.png";
        private const string ToggleOffPath = KitDir + "ui_config_toggle_off_v1.png";
        private const string ChipNormalPath = KitDir + "ui_config_chip_normal_v1.png";
        private const string ChipSelectedPath = KitDir + "ui_config_chip_selected_v1.png";
        private const string IconNotePath = KitDir + "ui_config_icon_note_v1.png";
        private const string IconBrightnessPath = KitDir + "ui_config_icon_brightness_v1.png";
        private const string IconSkipPath = KitDir + "ui_config_icon_skip_v1.png";
        private const string IconDifficultyPath = KitDir + "ui_config_icon_difficulty_v1.png";
        private const string SpeakerMinPath = KitDir + "ui_config_speaker_min_v1.png";
        private const string SpeakerMaxPath = KitDir + "ui_config_speaker_max_v1.png";
        private const string BtnMinusPath = KitDir + "ui_config_btn_minus_v1.png";
        private const string BtnPlusPath = KitDir + "ui_config_btn_plus_v1.png";
        private const string CrystalShardA = "Assets/FracturedChorus/Art/UI/TitleScreen/SheetV1/ui_crystal_shard_a_v1.png";
        private const string CrystalShardB = "Assets/FracturedChorus/Art/UI/TitleScreen/SheetV1/ui_crystal_shard_b_v1.png";
        private const string CrystalShardC = "Assets/FracturedChorus/Art/UI/TitleScreen/SheetV1/ui_crystal_shard_c_v1.png";

        [MenuItem("Fractured Chorus/Apply Config UI Kit")]
        public static void ApplyToOpenScene()
        {
            if (!Apply(setPreview: true))
            {
                EditorUtility.DisplayDialog(
                    "Apply Config UI Kit",
                    "Open MainMenuStartGame and ensure SettingsOverlay / ConfigUiRoot exist.",
                    "OK");
                return;
            }

            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("[Fractured Chorus] Config UI kit applied. Adjust RectTransforms in Scene, then Save.");
        }

        public static bool Apply(bool setPreview)
        {
            var overlay = GameObject.Find("SettingsOverlay");
            var uiRoot = GameObject.Find("ConfigUiRoot")?.GetComponent<RectTransform>();
            var list = GameObject.Find("ConfigList")?.transform;
            var controller = overlay != null ? overlay.GetComponent<MainMenuConfigOverlayController>() : null;
            var screen = Object.FindAnyObjectByType<MainMenuStartGameController>();
            if (overlay == null || uiRoot == null || list == null || controller == null)
            {
                return false;
            }

            Undo.RegisterFullObjectHierarchyUndo(overlay, "Apply Config UI Kit");

            EnsurePanel(uiRoot);
            EnsureConfigCrystalField(overlay.transform);
            StyleVolumeRow(list.Find("Row_Volume"));
            StyleBrightnessRow(list.Find("Row_Background_Brightness"));
            StyleSkipRow(list.Find("Row_Skip_Unread_Text"));
            StyleDifficultyRow(list.Find("Row_Difficulty"));
            StyleHighlight(list);
            StyleBackButton(uiRoot.Find("Footer/Btn_Back"));
            WireController(controller, screen, list);

            if (setPreview && screen != null)
            {
                screen.SetEditorPreview(MainMenuStartGameController.MainMenuEditorPreview.Settings);
                EditorUtility.SetDirty(screen);
            }

            EditorUtility.SetDirty(overlay);
            EditorUtility.SetDirty(controller);
            return true;
        }

        private static void EnsurePanel(RectTransform uiRoot)
        {
            var panel = EnsureImageChild(uiRoot, "Panel", out var created);
            BindSprite(panel, PanelPath, Image.Type.Sliced, preserveAspect: false, raycast: false);
            if (created)
            {
                Stretch(panel.rectTransform, Vector2.zero, Vector2.one, new Vector2(12f, 18f), new Vector2(-12f, -18f));
                panel.rectTransform.SetAsFirstSibling();
            }
        }

        private static void EnsureConfigCrystalField(Transform overlay)
        {
            var existing = overlay.Find("CrystalField");
            var created = existing == null;
            var go = created ? new GameObject("CrystalField", typeof(RectTransform)) : existing.gameObject;
            if (created)
            {
                Undo.RegisterCreatedObjectUndo(go, "Create CrystalField");
                go.transform.SetParent(overlay, false);
                var rect = go.GetComponent<RectTransform>();
                Stretch(rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }

            var field = go.GetComponent<TitleAttractCrystalField>() ?? go.AddComponent<TitleAttractCrystalField>();
            field.Bind(
                new[] { LoadSprite(CrystalShardA), LoadSprite(CrystalShardB), LoadSprite(CrystalShardC) },
                16);
            var bg = overlay.Find("ConfigBackground");
            var sibling = bg != null ? bg.GetSiblingIndex() + 1 : 1;
            go.transform.SetSiblingIndex(sibling);
            EditorUtility.SetDirty(field);
        }

        private static void StyleVolumeRow(Transform row)
        {
            if (row == null)
            {
                return;
            }

            EnsureIcon(row, IconNotePath, new Vector2(44f, 0f), new Vector2(72f, 72f));
            EnsureGraphic(row, "SpeakerMin", SpeakerMinPath, new Vector2(128f, 0f), new Vector2(56f, 48f), raycast: false);
            StyleSlider(row.Find("Slider"));
            EnsureGraphic(row, "SpeakerMax", SpeakerMaxPath, new Vector2(500f, 0f), new Vector2(56f, 48f), raycast: false);
            EnsureButtonGraphic(row, "BtnMinus", BtnMinusPath, new Vector2(568f, 0f), new Vector2(44f, 44f));
            EnsureButtonGraphic(row, "BtnPlus", BtnPlusPath, new Vector2(620f, 0f), new Vector2(44f, 44f));
        }

        private static void StyleBrightnessRow(Transform row)
        {
            if (row == null)
            {
                return;
            }

            EnsureIcon(row, IconBrightnessPath, new Vector2(44f, 0f), new Vector2(72f, 72f));
            StyleSlider(row.Find("Slider"));
            EnsureButtonGraphic(row, "BtnMinus", BtnMinusPath, new Vector2(568f, 0f), new Vector2(44f, 44f));
            EnsureButtonGraphic(row, "BtnPlus", BtnPlusPath, new Vector2(620f, 0f), new Vector2(44f, 44f));
        }

        private static void StyleSkipRow(Transform row)
        {
            if (row == null)
            {
                return;
            }

            EnsureIcon(row, IconSkipPath, new Vector2(44f, 0f), new Vector2(72f, 72f));
            var slider = row.Find("Slider");
            if (slider == null)
            {
                return;
            }

            HideChild(slider, "Background");
            HideChild(slider, "Fill Area");
            HideChild(slider, "Handle Slide Area");
            var image = slider.GetComponent<Image>() ?? slider.gameObject.AddComponent<Image>();
            BindSprite(image, ToggleOffPath, Image.Type.Simple, preserveAspect: true, raycast: true);

            var toggle = slider.GetComponent<MainMenuConfigToggleSwitch>() ??
                         slider.gameObject.AddComponent<MainMenuConfigToggleSwitch>();
            var so = new SerializedObject(toggle);
            so.FindProperty("visualSlider").objectReferenceValue = slider.GetComponent<Slider>();
            so.FindProperty("graphic").objectReferenceValue = image;
            so.FindProperty("spriteOn").objectReferenceValue = LoadSprite(ToggleOnPath);
            so.FindProperty("spriteOff").objectReferenceValue = LoadSprite(ToggleOffPath);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(toggle);
        }

        private static void StyleDifficultyRow(Transform row)
        {
            if (row == null)
            {
                return;
            }

            EnsureIcon(row, IconDifficultyPath, new Vector2(44f, 0f), new Vector2(72f, 72f));
            SetActive(row.Find("Lt"), false);
            SetActive(row.Find("Gt"), false);
            SetActive(row.Find("Value"), false);

            EnsureChip(row, "Chip_OnBeat", "ON BEAT", new Vector2(196f, 0f));
            EnsureChip(row, "Chip_Cadence", "CADENCE", new Vector2(396f, 0f));
            EnsureChip(row, "Chip_OffBeat", "OFF-BEAT", new Vector2(596f, 0f));
        }

        private static void StyleHighlight(Transform list)
        {
            var highlightRect = FindNamed(list, "HighlightBar")?.GetComponent<RectTransform>();
            var highlight = highlightRect != null ? highlightRect.GetComponent<Image>() : null;
            if (highlight == null)
            {
                return;
            }

            BindSprite(highlight, ChipSelectedPath, Image.Type.Simple, preserveAspect: false, raycast: false);
            highlight.color = new Color(1f, 1f, 1f, 0.35f);
            SetActive(highlightRect.Find("BorderTop"), false);
            SetActive(highlightRect.Find("BorderBottom"), false);
        }

        private static void StyleBackButton(Transform back)
        {
            if (back == null)
            {
                return;
            }

            var image = back.GetComponent<Image>();
            if (image == null)
            {
                return;
            }

            BindSprite(image, ChipNormalPath, Image.Type.Simple, preserveAspect: false, raycast: true);
        }

        private static void StyleSlider(Transform slider)
        {
            if (slider == null)
            {
                return;
            }

            var background = slider.Find("Background")?.GetComponent<Image>();
            if (background != null)
            {
                background.gameObject.SetActive(true);
                BindSprite(background, SliderTrackPath, Image.Type.Sliced, preserveAspect: false, raycast: false);
            }

            var fillArea = slider.Find("Fill Area");
            if (fillArea != null)
            {
                fillArea.gameObject.SetActive(true);
            }

            var fill = slider.Find("Fill Area/Fill")?.GetComponent<Image>();
            if (fill != null)
            {
                BindSprite(fill, SliderFillPath, Image.Type.Sliced, preserveAspect: false, raycast: false);
                fill.type = Image.Type.Sliced;
                fill.fillCenter = true;
            }

            var handleArea = slider.Find("Handle Slide Area");
            if (handleArea != null)
            {
                handleArea.gameObject.SetActive(true);
            }

            var handle = slider.Find("Handle Slide Area/Handle")?.GetComponent<Image>();
            if (handle != null)
            {
                BindSprite(handle, SliderHandlePath, Image.Type.Simple, preserveAspect: true, raycast: true);
            }
        }

        private static void EnsureIcon(Transform row, string spritePath, Vector2 pos, Vector2 size)
        {
            EnsureGraphic(row, "Icon", spritePath, pos, size, raycast: false);
        }

        private static void EnsureGraphic(
            Transform parent,
            string name,
            string spritePath,
            Vector2 pos,
            Vector2 size,
            bool raycast)
        {
            var image = EnsureImageChild(parent, name, out var created);
            BindSprite(image, spritePath, Image.Type.Simple, preserveAspect: true, raycast: raycast);
            if (created)
            {
                SetFree(image.rectTransform, new Vector2(0f, 0.5f), size, pos);
            }
        }

        private static void EnsureButtonGraphic(
            Transform parent,
            string name,
            string spritePath,
            Vector2 pos,
            Vector2 size)
        {
            var image = EnsureImageChild(parent, name, out var created);
            BindSprite(image, spritePath, Image.Type.Simple, preserveAspect: true, raycast: true);
            var button = image.GetComponent<Button>() ?? image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            if (created)
            {
                SetFree(image.rectTransform, new Vector2(0f, 0.5f), size, pos);
            }
        }

        private static void EnsureChip(Transform row, string name, string label, Vector2 pos)
        {
            var image = EnsureImageChild(row, name, out var created);
            BindSprite(image, ChipNormalPath, Image.Type.Simple, preserveAspect: true, raycast: true);
            var button = image.GetComponent<Button>() ?? image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            if (created)
            {
                SetFree(image.rectTransform, new Vector2(0f, 0.5f), new Vector2(176f, 64f), pos);
            }

            var text = image.transform.Find("Label")?.GetComponent<Text>();
            if (text == null)
            {
                text = SceneFontSetupEditor.CreateUiText("Label", image.transform, label, 16, TextAnchor.MiddleCenter);
                Stretch(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            }

            text.text = label;
            text.raycastTarget = false;
        }

        private static void WireController(
            MainMenuConfigOverlayController controller,
            MainMenuStartGameController screen,
            Transform list)
        {
            var volumeRow = list.Find("Row_Volume");
            var brightnessRow = list.Find("Row_Background_Brightness");
            var skipRow = list.Find("Row_Skip_Unread_Text");
            var difficultyRow = list.Find("Row_Difficulty");
            var so = new SerializedObject(controller);
            so.FindProperty("screenController").objectReferenceValue = screen;
            so.FindProperty("chipNormalSprite").objectReferenceValue = LoadSprite(ChipNormalPath);
            so.FindProperty("chipSelectedSprite").objectReferenceValue = LoadSprite(ChipSelectedPath);
            so.FindProperty("volumeMinusButton").objectReferenceValue = volumeRow?.Find("BtnMinus")?.GetComponent<Button>();
            so.FindProperty("volumePlusButton").objectReferenceValue = volumeRow?.Find("BtnPlus")?.GetComponent<Button>();
            so.FindProperty("brightnessMinusButton").objectReferenceValue = brightnessRow?.Find("BtnMinus")?.GetComponent<Button>();
            so.FindProperty("brightnessPlusButton").objectReferenceValue = brightnessRow?.Find("BtnPlus")?.GetComponent<Button>();
            so.FindProperty("skipUnreadToggle").objectReferenceValue =
                skipRow?.Find("Slider")?.GetComponent<MainMenuConfigToggleSwitch>();

            var chips = so.FindProperty("difficultyChipButtons");
            var graphics = so.FindProperty("difficultyChipGraphics");
            chips.arraySize = 3;
            graphics.arraySize = 3;
            AssignChip(chips, graphics, 0, difficultyRow?.Find("Chip_OnBeat"));
            AssignChip(chips, graphics, 1, difficultyRow?.Find("Chip_Cadence"));
            AssignChip(chips, graphics, 2, difficultyRow?.Find("Chip_OffBeat"));
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignChip(SerializedProperty buttons, SerializedProperty graphics, int index, Transform chip)
        {
            buttons.GetArrayElementAtIndex(index).objectReferenceValue = chip?.GetComponent<Button>();
            graphics.GetArrayElementAtIndex(index).objectReferenceValue = chip?.GetComponent<Image>();
        }

        private static Image EnsureImageChild(Transform parent, string name, out bool created)
        {
            var existing = parent.Find(name);
            created = existing == null;
            GameObject go;
            if (existing == null)
            {
                go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                Undo.RegisterCreatedObjectUndo(go, "Create " + name);
                go.transform.SetParent(parent, false);
            }
            else
            {
                go = existing.gameObject;
                if (go.GetComponent<CanvasRenderer>() == null)
                {
                    go.AddComponent<CanvasRenderer>();
                }

                if (go.GetComponent<Image>() == null)
                {
                    go.AddComponent<Image>();
                }
            }

            return go.GetComponent<Image>();
        }

        private static Transform FindNamed(Transform root, string name)
        {
            if (root.name == name)
            {
                return root;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var found = FindNamed(root.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static void BindSprite(Image image, string path, Image.Type type, bool preserveAspect, bool raycast)
        {
            image.sprite = LoadSprite(path);
            image.type = type;
            image.preserveAspect = preserveAspect;
            image.raycastTarget = raycast;
            image.color = Color.white;
            image.fillCenter = true;
        }

        private static Sprite LoadSprite(string assetPath)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite != null)
            {
                return sprite;
            }

            var assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (var i = 0; i < assets.Length; i++)
            {
                if (assets[i] is Sprite found)
                {
                    return found;
                }
            }

            Debug.LogWarning("[Fractured Chorus] Sprite not found: " + assetPath);
            return null;
        }

        private static void HideChild(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child != null)
            {
                child.gameObject.SetActive(false);
            }
        }

        private static void SetActive(Transform target, bool active)
        {
            if (target != null && target.gameObject.activeSelf != active)
            {
                target.gameObject.SetActive(active);
            }
        }

        private static void Stretch(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void SetFree(RectTransform rect, Vector2 anchor, Vector2 size, Vector2 pos)
        {
            if (rect == null)
            {
                return;
            }

            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = pos;
            rect.localScale = Vector3.one;
        }
    }
}
#endif
