using System.Collections.Generic;
using FracturedChorus.Data;
using FracturedChorus.RunMap.Core;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.RunMap.UI
{
    [ExecuteAlways]
    public class CadenceMacroMapView : MonoBehaviour
    {
        [SerializeField] private CadenceMapLayoutSO layout;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private RectTransform territoryLayer;
        [SerializeField] private VaultTerritoryGraphic territoryTemplate;
        [SerializeField] private Text hintLabel;

        private readonly List<VaultTerritoryGraphic> _territories = new List<VaultTerritoryGraphic>();

        public RectTransform TerritoryLayerRect => territoryLayer != null ? territoryLayer : transform as RectTransform;

        public event System.Action<VaultFingerId> VaultSelected;

        public void Build(CadenceMapLayoutSO layoutOverride = null)
        {
            layout = layoutOverride != null ? layoutOverride : layout;
            if (layout == null)
            {
                Debug.LogError("[Fractured Chorus] CadenceMacroMapView: layout null.");
                return;
            }

            if (backgroundImage != null && layout.backgroundSprite != null)
            {
                backgroundImage.sprite = layout.backgroundSprite;
                backgroundImage.preserveAspect = false;
                backgroundImage.color = Color.white;
            }

            ClearTerritories();

            var parent = territoryLayer != null ? territoryLayer : transform as RectTransform;
            foreach (var entry in layout.territories)
            {
                var graphic = CreateTerritory(parent, entry);
                if (graphic == null)
                {
                    continue;
                }

                graphic.TerritoryClicked += OnTerritoryClicked;
                graphic.TerritoryHovered += OnTerritoryHovered;
                _territories.Add(graphic);
            }

            SetHint("Select a Vault to Resonance Dive.");
        }

        public void SetVaultUnlocked(VaultFingerId finger, bool unlocked)
        {
            foreach (var territory in _territories)
            {
                if (ToFingerId(territory.FingerId) == finger)
                {
                    territory.SetUnlocked(unlocked);
                }
            }
        }

        public void SetHint(string text)
        {
            if (hintLabel != null)
            {
                hintLabel.text = text;
            }
        }

        private void OnTerritoryClicked(VaultTerritoryGraphic graphic)
        {
            VaultSelected?.Invoke(ToFingerId(graphic.FingerId));
        }

        private void OnTerritoryHovered(VaultTerritoryGraphic graphic)
        {
            if (!graphic.Unlocked)
            {
                SetHint("Vault locked.");
                return;
            }

            SetHint($"Enter {graphic.FingerId} — click to Dive.");
        }

        private VaultTerritoryGraphic CreateTerritory(RectTransform parent, CadenceMapLayoutSO.TerritoryEntry entry)
        {
            VaultTerritoryGraphic graphic;
            if (territoryTemplate != null)
            {
                graphic = Instantiate(territoryTemplate, parent);
            }
            else
            {
                var go = new GameObject($"Territory_{entry.finger}", typeof(RectTransform), typeof(CanvasRenderer), typeof(VaultTerritoryGraphic));
                go.transform.SetParent(parent, false);
                graphic = go.GetComponent<VaultTerritoryGraphic>();
            }

            var rect = graphic.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            graphic.raycastTarget = true;
            graphic.ApplyEntry(entry);
            graphic.gameObject.SetActive(true);
            return graphic;
        }

        private void ClearTerritories()
        {
            foreach (var territory in _territories)
            {
                if (territory == null)
                {
                    continue;
                }

                territory.TerritoryClicked -= OnTerritoryClicked;
                territory.TerritoryHovered -= OnTerritoryHovered;
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    DestroyImmediate(territory.gameObject);
                    continue;
                }
#endif
                Destroy(territory.gameObject);
            }

            _territories.Clear();
        }

        private static VaultFingerId ToFingerId(CadenceMapLayoutSO.VaultFingerIdRef finger) =>
            (VaultFingerId)(int)finger;
    }
}
