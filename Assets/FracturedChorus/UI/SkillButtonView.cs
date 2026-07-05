using System;
using FracturedChorus.Combat.Units;
using FracturedChorus.Data;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.UI
{
    public class SkillButtonView : MonoBehaviour
    {
        private Button _button;
        private Text _label;
        private Action _onClick;

        public void Build(Transform parent, SkillDefinitionSO skill, Action onClick)
        {
            _onClick = onClick;

            var rect = gameObject.AddComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(140f, 36f);

            var image = gameObject.AddComponent<Image>();
            image.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);

            _button = gameObject.AddComponent<Button>();
            _button.onClick.AddListener(() => _onClick?.Invoke());

            var textGo = new GameObject("Label");
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.SetParent(transform, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            _label = textGo.AddComponent<Text>();
            _label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _label.fontSize = 14;
            _label.alignment = TextAnchor.MiddleCenter;
            _label.color = Color.white;
            _label.text = skill != null ? SkillUiNames.GetDisplayName(skill) : "Skill";
        }
    }
}
