using System;
using UnityEngine;

namespace FracturedChorus.Data
{
    [CreateAssetMenu(fileName = "PartyRoster", menuName = "Fractured Chorus/Party Status Bar Roster")]
    public class PartyStatusBarRosterSO : ScriptableObject
    {
        [Tooltip("Mỗi phần tử = một ô UI (trái → phải). Khớp StatBlock + Preset trong Resources.")]
        public PartyStatusBarSlotDefinition[] slots = Array.Empty<PartyStatusBarSlotDefinition>();
    }

    [Serializable]
    public class PartyStatusBarSlotDefinition
    {
        [Tooltip("Tên asset trong Resources/StatBlocks (vd: StatBlock_Tank).")]
        public string statBlockResourceName;

        [Tooltip("Tên asset trong Resources/UnitPresets (vd: Tank hoặc UnitPreset_Tank).")]
        public string unitPresetResourceName;

        [Tooltip("Khớp unit combat theo unitId / demoUnitKey (vd: tank, ren, mage).")]
        public string unitMatchKey;
    }
}
