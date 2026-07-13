using System;
using UnityEngine;

namespace FracturedChorus.Narrative.Vn
{
    [Serializable]
    public sealed class VnBeat
    {
        public VnBeatKind kind = VnBeatKind.Narration;
        public string speakerId;
        [TextArea(1, 6)] public string text;
        public string expression;
        public string bgId;
        public string bgmId;
        public string sfxId;
        public float bgmPitch;
        public float bgmStartTime;
        public float duration;
        public string[] setFlags;
        public bool showDateHud;
        public bool hideDateHud;
        public string dateHudDate;
        public string dateHudPhase;
        public bool dateHudFromMeta;
    }
}
