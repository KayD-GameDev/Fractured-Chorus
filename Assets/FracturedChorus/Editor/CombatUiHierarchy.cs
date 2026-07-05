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
                PartyCardLayout.ApplyElementBadgeRect(badgeRect);
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

        public static int LogMissingScriptsInActiveScene()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogWarning("[Fractured Chorus] No active loaded scene.");
                return 0;
            }

            var count = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                LogMissingScriptsRecursive(root.transform, ref count);
            }

            if (count == 0)
            {
                Debug.Log("[Fractured Chorus] No missing scripts found in active scene.");
            }
            else
            {
                Debug.Log($"[Fractured Chorus] Found {count} missing script slot(s) in active scene.");
            }

            return count;
        }

        public static int RemoveMissingScriptsInActiveScene()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogWarning("[Fractured Chorus] No active loaded scene.");
                return 0;
            }

            var removed = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                removed += RemoveMissingScriptsRecursive(root.transform);
            }

            if (removed > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log($"[Fractured Chorus] Removed {removed} missing script slot(s). Save scene (Ctrl+S).");
            }
            else
            {
                Debug.Log("[Fractured Chorus] No missing scripts to remove in active scene.");
            }

            return removed;
        }

        private static void LogMissingScriptsRecursive(Transform transform, ref int count)
        {
            var components = transform.GetComponents<Component>();
            for (var i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    count++;
                    Debug.LogWarning($"[Fractured Chorus] Missing script on '{GetTransformPath(transform)}' (component index {i}).", transform.gameObject);
                }
            }

            for (var i = 0; i < transform.childCount; i++)
            {
                LogMissingScriptsRecursive(transform.GetChild(i), ref count);
            }
        }

        private static int RemoveMissingScriptsRecursive(Transform transform)
        {
            var removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(transform.gameObject);
            for (var i = 0; i < transform.childCount; i++)
            {
                removed += RemoveMissingScriptsRecursive(transform.GetChild(i));
            }

            return removed;
        }

        private static string GetTransformPath(Transform transform)
        {
            if (transform.parent == null)
            {
                return transform.name;
            }

            return $"{GetTransformPath(transform.parent)}/{transform.name}";
        }

        public static void EnsurePartyCardsInHierarchy()
        {
            CleanupFixedPartyCardsInHierarchy();

            var partyBar = Object.FindAnyObjectByType<PartyStatusBarUIView>(FindObjectsInactive.Include);
            if (partyBar == null)
            {
                Debug.LogWarning("[Fractured Chorus] PartyStatusBarUI not found — run Add Party Status Bar first.");
                return;
            }

            partyBar.WireReferences();

            var cardsRow = partyBar.transform.Find("CardsRow") as RectTransform;
            if (cardsRow == null)
            {
                Debug.LogWarning("[Fractured Chorus] CardsRow missing under PartyStatusBarUI.");
                return;
            }

            SetPartyBarField(partyBar, "cardSpacing", PartyStatusBarUIView.DefaultCardSpacing);

            var rowLayout = cardsRow.GetComponent<HorizontalLayoutGroup>();
            if (rowLayout != null)
            {
                Undo.RecordObject(rowLayout, "Party card row spacing");
                rowLayout.spacing = PartyStatusBarUIView.DefaultCardSpacing;
                rowLayout.enabled = false;
                EditorUtility.SetDirty(rowLayout);
            }

            var template = partyBar.transform.Find("CardTemplate")?.GetComponent<PartyMemberCardView>();
            if (template != null)
            {
                UpgradePartyCardTemplate(template);
                template.gameObject.SetActive(false);
            }

            EditorUtility.SetDirty(partyBar);

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.IsValid() && scene.isLoaded)
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }

            Debug.Log("[Fractured Chorus] Party bar ready: CardTemplate only in CardsRow; runtime clones per party size. Save scene.");
        }

        public static void EnsureEnemyCardsInHierarchy()
        {
            CleanupFixedEnemyCardsInHierarchy();

            var enemyBar = Object.FindAnyObjectByType<EnemyStatusBarUIView>(FindObjectsInactive.Include);
            if (enemyBar == null)
            {
                Debug.LogWarning("[Fractured Chorus] EnemyStatusBarUI not found — run Add Enemy Status Bar first.");
                return;
            }

            enemyBar.WireReferences();

            var cardsRow = enemyBar.transform.Find("CardsRow") as RectTransform;
            if (cardsRow == null)
            {
                Debug.LogWarning("[Fractured Chorus] CardsRow missing under EnemyStatusBarUI.");
                return;
            }

            var rowLayout = cardsRow.GetComponent<HorizontalLayoutGroup>();
            if (rowLayout != null)
            {
                rowLayout.spacing = enemyBar.CardSpacing;
                rowLayout.enabled = false;
                EditorUtility.SetDirty(rowLayout);
            }

            var template = enemyBar.transform.Find("CardTemplate")?.GetComponent<PartyMemberCardView>();
            if (template != null)
            {
                UpgradePartyCardTemplate(template);
                template.gameObject.SetActive(false);
            }

            EditorUtility.SetDirty(enemyBar);

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.IsValid() && scene.isLoaded)
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }

            Debug.Log("[Fractured Chorus] Enemy bar ready: CardTemplate in Hierarchy; runtime clones per enemy count. Save scene.");
        }

        public static EnemyStatusBarUIView AddEnemyStatusBarToScene()
        {
            RenameBackgroundCanvasInScene();

            var canvasTransform = ResolveCombatCanvasTransform();
            if (canvasTransform == null)
            {
                Debug.LogWarning("[Fractured Chorus] CombatCanvas not found.");
                return null;
            }

            var enemyBar = TimelineHierarchyBuilder.BuildEnemyStatusBar(canvasTransform);
            EnsureEnemyCardsInHierarchy();

            var bootstrap = Object.FindAnyObjectByType<CombatPrototypeBootstrap>();
            if (bootstrap != null)
            {
                SetEnemyBarRef(bootstrap, "enemyStatusBarView", enemyBar);
            }

            var scene = canvasTransform.gameObject.scene;
            if (scene.IsValid() && scene.isLoaded)
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }

            Selection.activeGameObject = enemyBar.gameObject;
            Debug.Log("[Fractured Chorus] EnemyStatusBarUI ready under CombatCanvas (top-right). Save scene.");
            return enemyBar;
        }

        public static void CleanupFixedEnemyCardsInHierarchy()
        {
            var enemyBar = Object.FindAnyObjectByType<EnemyStatusBarUIView>(FindObjectsInactive.Include);
            if (enemyBar == null)
            {
                return;
            }

            var cardsRow = enemyBar.transform.Find("CardsRow");
            if (cardsRow == null)
            {
                return;
            }

            var template = enemyBar.transform.Find("CardTemplate");
            var removed = 0;

            for (var i = cardsRow.childCount - 1; i >= 0; i--)
            {
                var child = cardsRow.GetChild(i);
                if (template != null && child == template)
                {
                    continue;
                }

                if (child.GetComponent<PartyMemberCardView>() == null)
                {
                    continue;
                }

                Undo.DestroyObjectImmediate(child.gameObject);
                removed++;
            }

            if (removed > 0)
            {
                Debug.Log($"[Fractured Chorus] Removed {removed} fixed enemy card(s) from CardsRow — runtime clones from CardTemplate.");
            }
        }

        private static void SetEnemyBarRef(CombatPrototypeBootstrap bootstrap, string fieldName, Object value)
        {
            var so = new SerializedObject(bootstrap);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                return;
            }

            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetEnemyBarField(EnemyStatusBarUIView enemyBar, string fieldName, object value)
        {
            var so = new SerializedObject(enemyBar);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                return;
            }

            switch (value)
            {
                case bool boolValue:
                    prop.boolValue = boolValue;
                    break;
                case float floatValue:
                    prop.floatValue = floatValue;
                    break;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void CleanupFixedPartyCardsInHierarchy()
        {
            var partyBar = Object.FindAnyObjectByType<PartyStatusBarUIView>(FindObjectsInactive.Include);
            if (partyBar == null)
            {
                return;
            }

            var cardsRow = partyBar.transform.Find("CardsRow");
            if (cardsRow == null)
            {
                return;
            }

            var template = partyBar.transform.Find("CardTemplate");
            var removed = 0;

            for (var i = cardsRow.childCount - 1; i >= 0; i--)
            {
                var child = cardsRow.GetChild(i);
                if (template != null && child == template)
                {
                    continue;
                }

                if (child.GetComponent<PartyMemberCardView>() == null)
                {
                    continue;
                }

                Undo.DestroyObjectImmediate(child.gameObject);
                removed++;
            }

            if (removed > 0)
            {
                Debug.Log($"[Fractured Chorus] Removed {removed} fixed party card(s) from CardsRow — runtime will clone from CardTemplate.");
            }
        }

        private static void SetPartyBarField(PartyStatusBarUIView partyBar, string fieldName, object value)
        {
            var so = new SerializedObject(partyBar);
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                return;
            }

            switch (value)
            {
                case bool boolValue:
                    prop.boolValue = boolValue;
                    break;
                case float floatValue:
                    prop.floatValue = floatValue;
                    break;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [MenuItem("Fractured Chorus/Setup Skill Panel in Hierarchy")]
        public static void EnsureSkillPanelInHierarchy()
        {
            var canvas = ResolveCombatCanvasTransform();
            if (canvas == null)
            {
                Debug.LogWarning("[Fractured Chorus] CombatCanvas not found.");
                return;
            }

            var panel = Object.FindAnyObjectByType<SkillPanelUIView>(FindObjectsInactive.Include);
            if (panel == null)
            {
                panel = TimelineHierarchyBuilder.BuildSkillPanel(canvas);
                Undo.RegisterCreatedObjectUndo(panel.gameObject, "Setup Skill Panel");
            }
            else
            {
                panel.WireReferences();
            }

            EditorUtility.SetDirty(panel);
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.IsValid() && scene.isLoaded)
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }

            Debug.Log("[Fractured Chorus] Skill panel hierarchy wired (Radial + 3 slots). Save scene.");
        }
    }
}
#endif
