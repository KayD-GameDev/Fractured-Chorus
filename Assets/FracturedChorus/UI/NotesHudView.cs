using FracturedChorus.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.UI
{
    public sealed class NotesHudView : MonoBehaviour
    {
        [SerializeField] private Text label;

        public static NotesHudView Ensure(Transform canvasRoot)
        {
            if (canvasRoot == null)
            {
                return null;
            }

            var existing = canvasRoot.GetComponentInChildren<NotesHudView>(true);
            if (existing != null)
            {
                existing.Refresh();
                return existing;
            }

            var go = new GameObject("NotesHud", typeof(RectTransform), typeof(NotesHudView));
            go.transform.SetParent(canvasRoot, false);
            var view = go.GetComponent<NotesHudView>();
            view.Build();
            view.Refresh();
            return view;
        }

        private void OnEnable() => Refresh();

        private void Update()
        {
            if (Time.frameCount % 30 == 0)
            {
                Refresh();
            }
        }

        public void Refresh()
        {
            if (label == null)
            {
                return;
            }

            var notes = GameMetaSession.HasSession ? GameMetaSession.Current.Wallet.Notes : 0;
            label.text = $"Notes {notes}";
        }

        private void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = UiCanvasLayers.Hud;
            gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            gameObject.AddComponent<GraphicRaycaster>();

            var rect = GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-24f, -18f);
            rect.sizeDelta = new Vector2(220f, 40f);

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(transform, false);
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            label = textGo.GetComponent<Text>();
            UiFontCatalog.Apply(label, UiFontRole.Display, 22, FontStyle.Bold);
            label.alignment = TextAnchor.MiddleRight;
            label.color = new Color(0.85f, 0.95f, 1f, 1f);
            label.raycastTarget = false;
        }
    }
}
