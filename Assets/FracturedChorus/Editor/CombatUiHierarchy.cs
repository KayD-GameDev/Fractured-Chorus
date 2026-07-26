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
            RestoreClearCardTemplatesInScene();
        }

        /// <summary>
        /// Khôi phục CardTemplate clear-card (CardArt + BarStack + badge) sau khi scene bị mất Hierarchy.
        /// </summary>
        public static void RestoreClearCardTemplatesInScene()
        {
            var cards = Object.FindObjectsByType<PartyMemberCardView>(FindObjectsInactive.Include);
            if (cards.Length == 0)
            {
                Debug.Log("[Fractured Chorus] No PartyMemberCardView found in scene.");
                return;
            }

            foreach (var card in cards)
            {
                UpgradePartyCardTemplate(card, forceRestoreClearCard: true);
            }

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.IsValid() && scene.isLoaded)
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }

            Debug.Log(
                $"[Fractured Chorus] Restored clear-card Hierarchy on {cards.Length} CardTemplate(s). " +
                "Save scene (Ctrl+S).");
        }

        private static void UpgradePartyCardTemplate(PartyMemberCardView card, bool forceRestoreClearCard = false)
        {
            if (card == null)
            {
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(card.gameObject, "Restore Clear Card Template");

            var roleBadge = card.transform.Find("RoleBadge");
            if (roleBadge != null)
            {
                Undo.DestroyObjectImmediate(roleBadge.gameObject);
            }

            // Clear-card: không dùng Border/Avatar — chỉ CardArt.
            DisableLegacyChrome(card.transform.Find("Border")?.gameObject);
            DisableLegacyChrome(card.transform.Find("Avatar")?.gameObject);

            EnsureElementBadge(card.transform, IsEnemyCardTemplate(card));
            UpgradeHealthBar(card.transform);
            EnsureEmbeddedCardHierarchy(card.transform, forceRestoreClearCard);

            ApplyClearCardRootSize(card.transform as RectTransform);
            ReparentHealthAndPrepIntoBarStack(card.transform);
            EnsurePrepPipsSegmentStrip(card.transform);
            RestoreCardSiblingOrder(card.transform);
            WireCardViewFields(card);

            card.WireReferences();
            EditorUtility.SetDirty(card);
        }

        private static bool IsEnemyCardTemplate(PartyMemberCardView card)
        {
            return card != null && card.GetComponentInParent<EnemyStatusBarUIView>(true) != null;
        }

        private static void DisableLegacyChrome(GameObject go)
        {
            if (go == null)
            {
                return;
            }

            Undo.RecordObject(go, "Disable legacy card chrome");
            go.SetActive(false);
        }

        private static void ApplyClearCardRootSize(RectTransform cardRt)
        {
            if (cardRt == null)
            {
                return;
            }

            Undo.RecordObject(cardRt, "Clear card size");
            cardRt.sizeDelta = new Vector2(PartyCardLayout.CardWidth, PartyCardLayout.CardHeight);

            var layout = cardRt.GetComponent<LayoutElement>();
            if (layout == null)
            {
                layout = Undo.AddComponent<LayoutElement>(cardRt.gameObject);
            }

            Undo.RecordObject(layout, "Clear card LayoutElement");
            layout.preferredWidth = PartyCardLayout.CardWidth;
            layout.preferredHeight = PartyCardLayout.CardHeight;
        }

        private static void EnsureElementBadge(Transform cardRoot, bool enemySide)
        {
            var badgeTransform = cardRoot.Find("ElementBadge") as RectTransform;
            if (badgeTransform == null)
            {
                var go = new GameObject("ElementBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                Undo.RegisterCreatedObjectUndo(go, "Create ElementBadge");
                badgeTransform = go.GetComponent<RectTransform>();
                badgeTransform.SetParent(cardRoot, false);
            }

            Undo.RecordObject(badgeTransform, "ElementBadge rect");
            badgeTransform.sizeDelta = new Vector2(PartyCardLayout.EmbeddedBadgeSize, PartyCardLayout.EmbeddedBadgeSize);
            badgeTransform.pivot = new Vector2(0.5f, 0.5f);
            if (enemySide)
            {
                // Góc trên-phải (mép ngoài bar quái).
                badgeTransform.anchorMin = new Vector2(1f, 1f);
                badgeTransform.anchorMax = new Vector2(1f, 1f);
                badgeTransform.anchoredPosition = new Vector2(-18f, -18f);
            }
            else
            {
                // Góc trên-trái (mép ngoài bar party) — khớp author gần nhất.
                badgeTransform.anchorMin = new Vector2(0f, 1f);
                badgeTransform.anchorMax = new Vector2(0f, 1f);
                badgeTransform.anchoredPosition = new Vector2(25f, -25f);
            }

            var ring = badgeTransform.GetComponent<Image>();
            if (ring == null)
            {
                ring = Undo.AddComponent<Image>(badgeTransform.gameObject);
            }

            Undo.RecordObject(ring, "ElementBadge circle ring");
            ring.sprite = UiCircleSpriteUtil.Circle;
            ring.type = Image.Type.Simple;
            ring.preserveAspect = true;
            ring.color = HarmonyElementPalette.GetBadgeRingColor(Combat.Damage.HarmonyElement.Melody);
            ring.raycastTarget = false;

            var iconTransform = badgeTransform.Find("ElementIcon");
            Image iconImage;
            if (iconTransform == null)
            {
                var iconGo = new GameObject("ElementIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                Undo.RegisterCreatedObjectUndo(iconGo, "Create ElementIcon");
                iconGo.transform.SetParent(badgeTransform, false);
                var iconRect = iconGo.GetComponent<RectTransform>();
                iconRect.anchorMin = Vector2.zero;
                iconRect.anchorMax = Vector2.one;
                iconRect.offsetMin = new Vector2(4f, 4f);
                iconRect.offsetMax = new Vector2(-4f, -4f);
                iconImage = iconGo.GetComponent<Image>();
            }
            else
            {
                iconImage = iconTransform.GetComponent<Image>();
                if (iconImage == null)
                {
                    iconImage = Undo.AddComponent<Image>(iconTransform.gameObject);
                }
            }

            Undo.RecordObject(iconImage, "ElementIcon circle");
            iconImage.sprite = UiCircleSpriteUtil.Circle;
            iconImage.type = Image.Type.Simple;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
        }

        /// <summary>Hierarchy: PrepPips = 3 đoạn chữ nhật (Pip_0..2), không còn pip tròn.</summary>
        private static void EnsurePrepPipsSegmentStrip(Transform cardRoot)
        {
            if (cardRoot == null)
            {
                return;
            }

            var prep = cardRoot.Find("BarStack/GaugeSlot/PrepPips") as RectTransform
                       ?? cardRoot.Find("PrepPips") as RectTransform;
            if (prep == null)
            {
                var gauge = cardRoot.Find("BarStack/GaugeSlot") as RectTransform ?? cardRoot as RectTransform;
                var go = new GameObject("PrepPips", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(go, "Create PrepPips");
                prep = go.GetComponent<RectTransform>();
                prep.SetParent(gauge, false);
                prep.anchorMin = Vector2.zero;
                prep.anchorMax = Vector2.one;
                prep.offsetMin = Vector2.zero;
                prep.offsetMax = Vector2.zero;
            }

            if (prep.GetComponent<PrepPipsView>() == null)
            {
                Undo.AddComponent<PrepPipsView>(prep.gameObject);
            }

            const int cap = 3;
            const float gap = 1.5f;
            for (var i = 0; i < cap; i++)
            {
                var pip = prep.Find($"Pip_{i}") as RectTransform;
                if (pip == null)
                {
                    var pipGo = new GameObject($"Pip_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    Undo.RegisterCreatedObjectUndo(pipGo, "Create Prep Pip");
                    pip = pipGo.GetComponent<RectTransform>();
                    pip.SetParent(prep, false);
                }

                Undo.RecordObject(pip, "Prep pip segment rect");
                var unit = 1f / cap;
                pip.anchorMin = new Vector2(i * unit, 0f);
                pip.anchorMax = new Vector2((i + 1) * unit, 1f);
                pip.pivot = new Vector2(0.5f, 0.5f);
                pip.anchoredPosition = Vector2.zero;
                pip.sizeDelta = Vector2.zero;
                pip.offsetMin = new Vector2(i > 0 ? gap * 0.5f : 0f, 0f);
                pip.offsetMax = new Vector2(i < cap - 1 ? -gap * 0.5f : 0f, 0f);
                pip.localScale = Vector3.one;
                pip.localRotation = Quaternion.identity;

                var img = pip.GetComponent<Image>();
                if (img == null)
                {
                    img = Undo.AddComponent<Image>(pip.gameObject);
                }

                Undo.RecordObject(img, "Prep pip rect sprite");
                img.sprite = UiCircleSpriteUtil.White;
                img.type = Image.Type.Simple;
                img.preserveAspect = false;
                img.raycastTarget = false;
                img.color = new Color(0.12f, 0.14f, 0.18f, 0.75f);
            }

            // Xóa pip thừa Pip_3+
            for (var i = prep.childCount - 1; i >= 0; i--)
            {
                var child = prep.GetChild(i);
                if (child == null || !child.name.StartsWith("Pip_"))
                {
                    continue;
                }

                if (!int.TryParse(child.name.Substring(4), out var index) || index < 0 || index >= cap)
                {
                    Undo.DestroyObjectImmediate(child.gameObject);
                }
            }

            EditorUtility.SetDirty(prep.gameObject);
        }

        /// <summary>
        /// Ensures CardArt + BarStack/HealthSlot/GaugeSlot exist on CardTemplate for EmbeddedBars skin.
        /// </summary>
        public static void EnsureEmbeddedCardHierarchy(Transform cardRoot)
        {
            EnsureEmbeddedCardHierarchy(cardRoot, forceRestore: false);
        }

        public static void EnsureEmbeddedCardHierarchy(Transform cardRoot, bool forceRestore)
        {
            if (cardRoot == null)
            {
                return;
            }

            var isEnemy = cardRoot.GetComponentInParent<EnemyStatusBarUIView>(true) != null;

            var cardArt = cardRoot.Find("CardArt");
            if (cardArt == null)
            {
                var go = new GameObject("CardArt", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                Undo.RegisterCreatedObjectUndo(go, "Create CardArt");
                var rt = go.GetComponent<RectTransform>();
                rt.SetParent(cardRoot, false);
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                var image = go.GetComponent<Image>();
                image.raycastTarget = false;
                image.preserveAspect = false;
                go.SetActive(true);
                cardArt = go.transform;
            }
            else if (forceRestore)
            {
                var rt = cardArt as RectTransform;
                Undo.RecordObject(rt, "CardArt stretch");
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                cardArt.gameObject.SetActive(true);
            }

            var barStack = cardRoot.Find("BarStack") as RectTransform;
            var createdBarStack = false;
            if (barStack == null)
            {
                var go = new GameObject("BarStack", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(go, "Create BarStack");
                barStack = go.GetComponent<RectTransform>();
                barStack.SetParent(cardRoot, false);
                createdBarStack = true;
            }

            if (createdBarStack || forceRestore)
            {
                ApplyAuthoredBarStackRect(barStack, isEnemy);
                barStack.gameObject.SetActive(true);
            }

            var healthSlot = barStack.Find("HealthSlot") as RectTransform;
            var createdHealth = false;
            if (healthSlot == null)
            {
                var go = new GameObject("HealthSlot", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(go, "Create HealthSlot");
                healthSlot = go.GetComponent<RectTransform>();
                healthSlot.SetParent(barStack, false);
                createdHealth = true;
            }

            var gaugeSlot = barStack.Find("GaugeSlot") as RectTransform;
            var createdGauge = false;
            if (gaugeSlot == null)
            {
                var go = new GameObject("GaugeSlot", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(go, "Create GaugeSlot");
                gaugeSlot = go.GetComponent<RectTransform>();
                gaugeSlot.SetParent(barStack, false);
                createdGauge = true;
            }

            if (createdHealth || createdGauge || forceRestore)
            {
                PartyCardLayout.ApplyEmbeddedHealthSlotRect(healthSlot, gaugeSlot);
            }
        }

        /// <summary>BarStack geometry từ author gần nhất trước khi scene bị restore.</summary>
        private static void ApplyAuthoredBarStackRect(RectTransform barStack, bool enemySide)
        {
            if (barStack == null)
            {
                return;
            }

            Undo.RecordObject(barStack, "BarStack authored rect");
            barStack.anchorMin = new Vector2(0f, 0f);
            barStack.anchorMax = new Vector2(0f, 0f);
            barStack.pivot = new Vector2(0.5f, 0.5f);
            barStack.localScale = Vector3.one;

            if (enemySide)
            {
                barStack.anchoredPosition = new Vector2(73.41f, 28.1f);
                barStack.sizeDelta = new Vector2(124.53f, 32.98f);
                barStack.localRotation = Quaternion.Euler(0f, 0f, -10.141f);
            }
            else
            {
                barStack.anchoredPosition = new Vector2(75.51f, 41.75f);
                barStack.sizeDelta = new Vector2(124.59f, 37.09f);
                barStack.localRotation = Quaternion.Euler(0f, 0f, -12.07f);
            }
        }

        private static void ReparentHealthAndPrepIntoBarStack(Transform cardRoot)
        {
            var healthSlot = cardRoot.Find("BarStack/HealthSlot") as RectTransform;
            var gaugeSlot = cardRoot.Find("BarStack/GaugeSlot") as RectTransform;

            var healthBg = cardRoot.Find("HealthBarBg") as RectTransform
                           ?? cardRoot.Find("BarStack/HealthSlot/HealthBarBg") as RectTransform;
            if (healthBg != null && healthSlot != null && healthBg.parent != healthSlot)
            {
                Undo.SetTransformParent(healthBg, healthSlot, "HealthBar into HealthSlot");
                StretchFull(healthBg);
            }

            var prep = cardRoot.Find("PrepPips") as RectTransform
                       ?? cardRoot.Find("BarStack/GaugeSlot/PrepPips") as RectTransform;
            if (prep != null && gaugeSlot != null && prep.parent != gaugeSlot)
            {
                Undo.SetTransformParent(prep, gaugeSlot, "PrepPips into GaugeSlot");
                StretchFull(prep);
            }
        }

        private static void RestoreCardSiblingOrder(Transform cardRoot)
        {
            // BarStack trên CardArt trong Hierarchy; ElementBadge cuối để không bị art che.
            cardRoot.Find("BarStack")?.SetAsFirstSibling();
            var cardArt = cardRoot.Find("CardArt");
            if (cardArt != null)
            {
                cardArt.SetSiblingIndex(1);
            }

            cardRoot.Find("ElementBadge")?.SetAsLastSibling();
        }

        private static void WireCardViewFields(PartyMemberCardView card)
        {
            var so = new SerializedObject(card);
            SetObjectRef(so, "cardArtImage", card.transform.Find("CardArt")?.GetComponent<Image>());
            SetObjectRef(so, "barStack", card.transform.Find("BarStack") as RectTransform);
            SetObjectRef(so, "healthSlot", card.transform.Find("BarStack/HealthSlot") as RectTransform);
            SetObjectRef(so, "gaugeSlot", card.transform.Find("BarStack/GaugeSlot") as RectTransform);
            SetObjectRef(so, "elementBadgeRing", card.transform.Find("ElementBadge")?.GetComponent<Image>());
            SetObjectRef(so, "elementIcon", card.transform.Find("ElementBadge/ElementIcon")?.GetComponent<Image>());
            SetObjectRef(so, "healthBarBg",
                card.transform.Find("BarStack/HealthSlot/HealthBarBg")?.GetComponent<Image>()
                ?? card.transform.Find("HealthBarBg")?.GetComponent<Image>());
            var fill = card.transform.Find("BarStack/HealthSlot/HealthBarBg/HealthBarFill")?.GetComponent<Image>()
                       ?? card.transform.Find("HealthBarBg/HealthBarFill")?.GetComponent<Image>();
            SetObjectRef(so, "healthBarFill", fill);
            SetObjectRef(so, "healthBarFillRect", fill != null ? fill.rectTransform : null);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectRef(SerializedObject so, string field, Object value)
        {
            var prop = so.FindProperty(field);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
            }
        }

        private static void StretchFull(RectTransform rt)
        {
            if (rt == null)
            {
                return;
            }

            Undo.RecordObject(rt, "Stretch full");
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
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
                UpgradePartyCardTemplate(template, forceRestoreClearCard: true);
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
                UpgradePartyCardTemplate(template, forceRestoreClearCard: true);
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
                TimelineHierarchyBuilder.MigrateExistingSkillPanel(panel);
            }

            EditorUtility.SetDirty(panel);
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.IsValid() && scene.isLoaded)
            {
                EditorSceneManager.MarkSceneDirty(scene);
            }

            Debug.Log("[Fractured Chorus] Skill panel hierarchy wired (Radial + SkillSlot_Template + 3 slots, Frame above art). Save scene.");
        }

        /// <summary>
        /// Batch/menu: mở CombatPrototype, thêm SkillSlot_Template + Frame trên art, save scene.
        /// </summary>
        [MenuItem("Fractured Chorus/Migrate Skill Slot Template (CombatPrototype)")]
        public static void MigrateSkillSlotTemplateCombatPrototype()
        {
            const string scenePath = "Assets/FracturedChorus/Scenes/CombatPrototype.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var panel = Object.FindAnyObjectByType<SkillPanelUIView>(FindObjectsInactive.Include);
            if (panel == null)
            {
                Debug.LogWarning("[Fractured Chorus] SkillPanelUIView not found in CombatPrototype.");
                return;
            }

            TimelineHierarchyBuilder.EnsureSkillChromeTemplateOnPanel(panel);
            EditorUtility.SetDirty(panel);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Fractured Chorus] Migrated SkillSlot_Template + Frame-above-art on CombatPrototype. Scene saved.");
        }
    }
}
#endif
