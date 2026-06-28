using System;
using FracturedChorus.RunMap.Core;
using FracturedChorus.UI;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.RunMap.UI
{
    public class MapNodeView : MonoBehaviour
    {
        [SerializeField] private Image fillImage;
        [SerializeField] private Image strokeImage;
        [SerializeField] private Text labelText;
        [SerializeField] private Button button;

        public MapNodeData BoundNode { get; private set; }
        public event Action<MapNodeView> Clicked;

        private void Awake()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (button != null)
            {
                button.onClick.AddListener(() => Clicked?.Invoke(this));
            }
        }

        public void Bind(MapNodeData node)
        {
            BoundNode = node;
            RefreshVisual();
        }

        public void RefreshVisual(bool reachable = false, bool onPath = false, bool current = false)
        {
            if (BoundNode == null)
            {
                return;
            }

            var fill = MapNodePalette.FillColor(BoundNode.Type);
            var stroke = MapNodePalette.StrokeColor(BoundNode.Type);

            if (BoundNode.Cleared)
            {
                fill.a = 0.35f;
            }
            else if (current)
            {
                stroke = new Color(0.9f, 0.49f, 0.13f);
            }
            else if (onPath)
            {
                stroke = new Color(0.9f, 0.49f, 0.13f);
            }
            else if (!reachable)
            {
                fill.a = 0.55f;
            }

            if (fillImage != null)
            {
                fillImage.color = fill;
                if (fillImage.sprite == null)
                {
                    fillImage.sprite = UiCircleSpriteUtil.Circle;
                }
            }

            if (strokeImage != null)
            {
                strokeImage.color = stroke;
                if (strokeImage.sprite == null)
                {
                    strokeImage.sprite = UiCircleSpriteUtil.Circle;
                }
            }

            if (labelText != null)
            {
                labelText.text = MapNodePalette.Label(BoundNode.Type);
                labelText.color = BoundNode.Type == MapNodeType.Boss ? Color.white : new Color(0.2f, 0.2f, 0.25f);
                labelText.fontSize = BoundNode.IsBoss ? 22 : 14;
                labelText.fontStyle = BoundNode.IsBoss ? FontStyle.Bold : FontStyle.Normal;
            }

            if (button != null)
            {
                button.interactable = reachable && !BoundNode.Cleared;
            }
        }

        public void WireImages(Image fill, Image stroke, Text label, Button btn)
        {
            fillImage = fill;
            strokeImage = stroke;
            labelText = label;
            button = btn;
        }
    }
}
