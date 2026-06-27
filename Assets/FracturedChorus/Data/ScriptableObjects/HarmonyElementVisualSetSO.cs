using System;
using FracturedChorus.Combat.Damage;
using UnityEngine;

namespace FracturedChorus.Data
{
    [CreateAssetMenu(fileName = "HarmonyElementVisualSet", menuName = "Fractured Chorus/Harmony Element Visual Set")]
    public class HarmonyElementVisualSetSO : ScriptableObject
    {
        public ElementVisualEntry[] entries = Array.Empty<ElementVisualEntry>();

        public Sprite GetIcon(HarmonyElement element)
        {
            if (entries == null)
            {
                return null;
            }

            foreach (var entry in entries)
            {
                if (entry.element == element)
                {
                    return entry.icon;
                }
            }

            return null;
        }
    }

    [Serializable]
    public struct ElementVisualEntry
    {
        public HarmonyElement element;
        public Sprite icon;
    }
}
