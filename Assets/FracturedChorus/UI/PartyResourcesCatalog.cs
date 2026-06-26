using FracturedChorus.Data;
using UnityEngine;

namespace FracturedChorus.UI
{
    public static class PartyResourcesCatalog
    {
        private const string StatBlocksFolder = "StatBlocks";
        private const string UnitPresetsFolder = "UnitPresets";

        public static UnitStatBlockSO LoadStatBlock(string resourceName)
        {
            if (string.IsNullOrWhiteSpace(resourceName))
            {
                return null;
            }

            return Resources.Load<UnitStatBlockSO>($"{StatBlocksFolder}/{resourceName}");
        }

        public static UnitPresetSO LoadUnitPreset(string resourceName)
        {
            if (string.IsNullOrWhiteSpace(resourceName))
            {
                return null;
            }

            var assetName = resourceName.StartsWith("UnitPreset_")
                ? resourceName
                : $"UnitPreset_{resourceName}";

            return Resources.Load<UnitPresetSO>($"{UnitPresetsFolder}/{assetName}");
        }

        public static PartyStatusBarRosterSO LoadRoster(string resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                return null;
            }

            return Resources.Load<PartyStatusBarRosterSO>(resourcePath);
        }

        public static HarmonyElementVisualSetSO LoadElementVisualSet(string resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                return null;
            }

            return Resources.Load<HarmonyElementVisualSetSO>(resourcePath);
        }
    }
}
