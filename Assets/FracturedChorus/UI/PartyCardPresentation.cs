using FracturedChorus.Combat.Damage;
using FracturedChorus.Data;
using UnityEngine;

namespace FracturedChorus.UI
{
    public sealed class PartyCardPresentation
    {
        public UnitStatBlockSO StatBlock { get; set; }
        public UnitPresetSO Preset { get; set; }
        public Sprite Avatar { get; set; }
        public Sprite ElementIcon { get; set; }
        public HarmonyElement Element { get; set; }
        public string UnitMatchKey { get; set; }
    }
}
