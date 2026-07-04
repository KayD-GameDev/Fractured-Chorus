using System;
using UnityEngine;

namespace FracturedChorus.Data
{
    [CreateAssetMenu(fileName = "CadenceMapLayout", menuName = "Fractured Chorus/Cadence Map Layout")]
    public class CadenceMapLayoutSO : ScriptableObject
    {
        public Sprite backgroundSprite;
        public TerritoryEntry[] territories = DefaultTerritories();

        [Serializable]
        public struct TerritoryEntry
        {
            public string displayName;
            public VaultFingerIdRef finger;
            public Color territoryColor;
            public Color highlightColor;
            public bool unlocked;
            public Vector2[] normalizedVertices;
        }

        public enum VaultFingerIdRef
        {
            Thumb = 0,
            Index = 1,
            Middle = 2,
            Ring = 3,
            Pinky = 4
        }

        public static TerritoryEntry[] DefaultTerritories() => new[]
        {
            new TerritoryEntry
            {
                displayName = "Vault — Pinky",
                finger = VaultFingerIdRef.Pinky,
                territoryColor = new Color(0.92f, 0.22f, 0.58f, 0.22f),
                highlightColor = new Color(1f, 0.35f, 0.72f, 0.48f),
                unlocked = true,
                normalizedVertices = new[]
                {
                    new Vector2(0.02f, 0.48f),
                    new Vector2(0.10f, 0.86f),
                    new Vector2(0.36f, 0.94f),
                    new Vector2(0.44f, 0.66f),
                    new Vector2(0.30f, 0.46f)
                }
            },
            new TerritoryEntry
            {
                displayName = "Vault — Thumb",
                finger = VaultFingerIdRef.Thumb,
                territoryColor = new Color(0.78f, 0.12f, 0.10f, 0.22f),
                highlightColor = new Color(1f, 0.28f, 0.18f, 0.45f),
                unlocked = false,
                normalizedVertices = new[]
                {
                    new Vector2(0.36f, 0.94f),
                    new Vector2(0.50f, 1.00f),
                    new Vector2(0.64f, 0.94f),
                    new Vector2(0.58f, 0.66f),
                    new Vector2(0.44f, 0.66f)
                }
            },
            new TerritoryEntry
            {
                displayName = "Vault — Index",
                finger = VaultFingerIdRef.Index,
                territoryColor = new Color(0.95f, 0.48f, 0.10f, 0.22f),
                highlightColor = new Color(1f, 0.62f, 0.18f, 0.45f),
                unlocked = false,
                normalizedVertices = new[]
                {
                    new Vector2(0.64f, 0.94f),
                    new Vector2(0.90f, 0.86f),
                    new Vector2(0.98f, 0.50f),
                    new Vector2(0.76f, 0.44f),
                    new Vector2(0.58f, 0.66f)
                }
            },
            new TerritoryEntry
            {
                displayName = "Vault — Middle",
                finger = VaultFingerIdRef.Middle,
                territoryColor = new Color(0.55f, 0.78f, 0.18f, 0.22f),
                highlightColor = new Color(0.72f, 0.95f, 0.28f, 0.45f),
                unlocked = false,
                normalizedVertices = new[]
                {
                    new Vector2(0.76f, 0.44f),
                    new Vector2(0.98f, 0.50f),
                    new Vector2(0.94f, 0.10f),
                    new Vector2(0.54f, 0.06f),
                    new Vector2(0.52f, 0.30f)
                }
            },
            new TerritoryEntry
            {
                displayName = "Vault — Ring",
                finger = VaultFingerIdRef.Ring,
                territoryColor = new Color(0.12f, 0.72f, 0.82f, 0.22f),
                highlightColor = new Color(0.28f, 0.92f, 1f, 0.45f),
                unlocked = false,
                normalizedVertices = new[]
                {
                    new Vector2(0.02f, 0.48f),
                    new Vector2(0.30f, 0.46f),
                    new Vector2(0.52f, 0.30f),
                    new Vector2(0.54f, 0.06f),
                    new Vector2(0.06f, 0.08f)
                }
            }
        };
    }
}
