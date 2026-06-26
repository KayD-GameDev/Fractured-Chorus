#if UNITY_EDITOR
using FracturedChorus.Combat.Bootstrap;
using FracturedChorus.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Editor
{
    public static class CombatUiHierarchy
    {
        public const string CombatCanvasName = "CombatCanvas";
        public const string BackgroundCanvasName = "Background canvas";

        public static Transform ResolveCombatCanvasTransform()
        {
            var combatRoot = GameObject.Find("CombatRoot");
            if (combatRoot != null)
            {
                var underRoot = combatRoot.transform.Find(CombatCanvasName);
                if (underRoot != null)
                {
                    return underRoot;
                }
            }

            var byName = GameObject.Find(CombatCanvasName);
            if (byName != null)
            {
                return byName.transform;
            }

            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
            {
                if (canvas.name == CombatCanvasName)
                {
                    return canvas.transform;
                }
            }

            return Object.FindAnyObjectByType<Canvas>()?.transform;
        }

        public static Canvas ResolveCombatCanvas()
        {
            var transform = ResolveCombatCanvasTransform();
            return transform != null ? transform.GetComponent<Canvas>() : null;
        }

        public static void RenameBackgroundCanvasInScene()
        {
            var combatRoot = GameObject.Find("CombatRoot");
            Transform bgTransform = null;

            if (combatRoot != null)
            {
                foreach (Transform child in combatRoot.transform)
                {
                    if (child.name == "Canvas" && child.GetComponent<Canvas>() != null)
                    {
                        bgTransform = child;
                        break;
                    }

                    if (child.name == BackgroundCanvasName && child.GetComponent<Canvas>() != null)
                    {
                        return;
                    }
                }
            }

            if (bgTransform == null)
            {
                foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
                {
                    if (canvas.name == "Canvas" && canvas.name != CombatCanvasName)
                    {
                        bgTransform = canvas.transform;
                        break;
                    }
                }
            }

            if (bgTransform == null || bgTransform.name == BackgroundCanvasName)
            {
                return;
            }

            Undo.RecordObject(bgTransform.gameObject, "Rename Background Canvas");
            bgTransform.name = BackgroundCanvasName;
            EditorUtility.SetDirty(bgTransform.gameObject);
        }

        public static bool MovePartyStatusBarToCombatCanvas()
        {
            var partyBar = Object.FindAnyObjectByType<PartyStatusBarUIView>(FindObjectsInactive.Include);
            if (partyBar == null)
            {
                return false;
            }

            var combatCanvas = ResolveCombatCanvasTransform();
            if (combatCanvas == null)
            {
                return false;
            }

            if (partyBar.transform.parent == combatCanvas)
            {
                return false;
            }

            Undo.SetTransformParent(partyBar.transform, combatCanvas, "Move PartyStatusBarUI to CombatCanvas");
            partyBar.transform.SetAsFirstSibling();
            EditorUtility.SetDirty(partyBar);

            var bootstrap = Object.FindAnyObjectByType<CombatPrototypeBootstrap>();
            if (bootstrap != null)
            {
                var so = new SerializedObject(bootstrap);
                var prop = so.FindProperty("partyStatusBarView");
                if (prop != null)
                {
                    prop.objectReferenceValue = partyBar;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(bootstrap);
                }
            }

            return true;
        }

        public static void UpgradePartyCardTemplatesInScene()
        {
            var cards = Object.FindObjectsByType<PartyMemberCardView>(FindObjectsInactive.Include);
            if (cards.Length == 0)
            {
                Debug.Log("[Fractured Chorus] No PartyMemberCardView found in scene.");
                return;
            }

            foreach (var card in cards)
            {
                UpgradePartyCardTemplate(card);
            }

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.IsValid() && scene.isLoaded)
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }

            Debug.Log($"[Fractured Chorus] Upgraded {cards.Length} party card template(s). Save scene (Ctrl+S).");
        }

        private static void UpgradePartyCardTemplate(PartyMemberCardView card)
        {
            var roleBadge = card.transform.Find("RoleBadge");
            if (roleBadge != null)
            {
                Undo.DestroyObjectImmediate(roleBadge.gameObject);
            }

            var badgeTransform = card.transform.Find("ElementBadge");
            if (badgeTransform == null)
            {
                return;
            }

            var ring = badgeTransform.GetComponent<Image>();
            if (ring == null)
            {
                ring = Undo.AddComponent<Image>(badgeTransform.gameObject);
            }

            ring.sprite = UiCircleSpriteUtil.Circle;
            ring.raycastTarget = false;

            var iconTransform = badgeTransform.Find("ElementIcon");
            Image iconImage;
            if (iconTransform == null)
            {
                var iconGo = new GameObject("ElementIcon", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(iconGo, "Create ElementIcon");
                iconGo.transform.SetParent(badgeTransform, false);
                var iconRect = iconGo.GetComponent<RectTransform>();
                iconRect.anchorMin = Vector2.zero;
                iconRect.anchorMax = Vector2.one;
                iconRect.offsetMin = new Vector2(4f, 4f);
                iconRect.offsetMax = new Vector2(-4f, -4f);
                iconImage = Undo.AddComponent<Image>(iconGo);
            }
            else
            {
                iconImage = iconTransform.GetComponent<Image>();
                if (iconImage == null)
                {
                    iconImage = Undo.AddComponent<Image>(iconTransform.gameObject);
                }
            }

            iconImage.sprite = UiCircleSpriteUtil.Circle;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            UpgradeHealthBar(card.transform);

            var badgeRect = badgeTransform as RectTransform;
            if (badgeRect != null)
            {
                badgeRect.anchorMin = new Vector2(1f, 1f);
                badgeRect.anchorMax = new Vector2(1f, 1f);
                badgeRect.pivot = new Vector2(0.5f, 0.5f);
                badgeRect.anchoredPosition = new Vector2(-6f, -6f);
                badgeRect.sizeDelta = new Vector2(22f, 22f);
            }

            card.WireReferences();
            EditorUtility.SetDirty(card);
        }

        private static void UpgradeHealthBar(Transform cardRoot)
        {
            var bgTransform = cardRoot.Find("HealthBarBg");
            if (bgTransform == null)
            {
                return;
            }

            var bgImage = bgTransform.GetComponent<Image>();
            if (bgImage == null)
            {
                bgImage = Undo.AddComponent<Image>(bgTransform.gameObject);
            }

            bgImage.sprite = UiCircleSpriteUtil.White;
            bgImage.type = Image.Type.Simple;
            bgImage.color = new Color(0.08f, 0.08f, 0.1f, 0.95f);
            bgImage.raycastTarget = false;

            var fillTransform = bgTransform.Find("HealthBarFill");
            if (fillTransform == null)
            {
                return;
            }

            var fillRect = fillTransform as RectTransform;
            var fillImage = fillTransform.GetComponent<Image>();
            if (fillImage == null)
            {
                fillImage = Undo.AddComponent<Image>(fillTransform.gameObject);
            }

            fillImage.sprite = UiCircleSpriteUtil.White;
            fillImage.type = Image.Type.Simple;
            fillImage.color = new Color(0.18f, 0.92f, 0.28f, 1f);
            fillImage.raycastTarget = false;

            if (fillRect != null)
            {
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = Vector2.one;
                fillRect.pivot = new Vector2(0f, 0.5f);
                fillRect.offsetMin = Vector2.zero;
                fillRect.offsetMax = Vector2.zero;
            }

            var bgRect = bgTransform as RectTransform;
            if (bgRect != null)
            {
                bgRect.sizeDelta = new Vector2(bgRect.sizeDelta.x, 10f);
            }
        }

        public static void FixPartyStatusBarPlacement()
        {
            RenameBackgroundCanvasInScene();
            var moved = MovePartyStatusBarToCombatCanvas();

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.IsValid() && scene.isLoaded)
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }

            if (moved)
            {
                Debug.Log("[Fractured Chorus] Moved PartyStatusBarUI under CombatCanvas. Save scene (Ctrl+S).");
            }
            else
            {
                Debug.Log("[Fractured Chorus] Background canvas renamed (if needed). PartyStatusBarUI already under CombatCanvas or not found.");
            }
        }
    }
}
#endif
