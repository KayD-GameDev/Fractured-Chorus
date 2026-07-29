using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Narrative.Vn
{
    public static class VnRuntimeUiLayoutApplier
    {
        public static void ApplyReadability(VnRuntimeController runtime)
        {
            if (runtime == null)
            {
                return;
            }

            ApplyDialogueReadability(runtime);
            ApplyTextCardReadability(runtime);
        }

        private static void ApplyDialogueReadability(VnRuntimeController runtime)
        {
            var panel = runtime.DialoguePanel;
            if (panel == null)
            {
                return;
            }

            var frame = panel.transform.Find("DialogueFrame")?.GetComponent<Image>();
            if (frame != null)
            {
                frame.gameObject.SetActive(true);
                frame.raycastTarget = false;
            }

            var body = FindDialogueBody(panel.transform, runtime);
            EnsureBodyBacking(panel.transform, body);

            VnUiFont.ApplyReadableEffectsOnly(runtime.NameplateText);
            VnUiFont.ApplyReadableEffectsOnly(body);
        }

        private static void ApplyTextCardReadability(VnRuntimeController runtime)
        {
            var cardPanel = runtime.TextCardPanel;
            if (cardPanel == null)
            {
                return;
            }

            VnUiFont.ApplyReadableEffectsOnly(runtime.TextCardBody);
        }

        private static Text FindDialogueBody(Transform dialoguePanel, VnRuntimeController runtime)
        {
            var body = dialoguePanel.Find("DialogueBody")?.GetComponent<Text>();
            if (body != null)
            {
                return body;
            }

            var typewriter = runtime.GetComponentInChildren<PrologueTypewriterView>(true);
            return typewriter != null ? typewriter.BodyText : null;
        }

        private static void EnsureBodyBacking(Transform dialoguePanel, Text body)
        {
            var existing = dialoguePanel.Find("DialogueBodyBacking");
            if (existing != null)
            {
                var image = existing.GetComponent<Image>();
                if (image != null)
                {
                    image.color = VnDialoguePanelLayout.BodyBackingColor;
                    image.raycastTarget = false;
                }

                existing.SetAsFirstSibling();
                return;
            }

            if (body == null)
            {
                return;
            }

            var go = new GameObject("DialogueBodyBacking", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(dialoguePanel, false);
            go.transform.SetAsFirstSibling();
            var backing = go.GetComponent<Image>();
            backing.color = VnDialoguePanelLayout.BodyBackingColor;
            backing.raycastTarget = false;
            FitBackingToBody(go.GetComponent<RectTransform>(), body.rectTransform);

            var frame = dialoguePanel.Find("DialogueFrame");
            if (frame != null)
            {
                frame.SetSiblingIndex(go.transform.GetSiblingIndex() + 1);
            }
        }

        private static void FitBackingToBody(RectTransform backing, RectTransform body)
        {
            backing.anchorMin = body.anchorMin;
            backing.anchorMax = body.anchorMax;
            backing.pivot = body.pivot;
            backing.anchoredPosition = body.anchoredPosition;
            backing.sizeDelta = body.sizeDelta;
            backing.offsetMin = body.offsetMin - new Vector2(12f, 10f);
            backing.offsetMax = body.offsetMax + new Vector2(12f, 10f);
            backing.localScale = body.localScale;
        }
    }
}
