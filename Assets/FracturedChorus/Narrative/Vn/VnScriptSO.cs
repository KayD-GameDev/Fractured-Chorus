using UnityEngine;

namespace FracturedChorus.Narrative.Vn
{
    [CreateAssetMenu(
        fileName = "VnScript_",
        menuName = "Fractured Chorus/Narrative/VN Script")]
    public sealed class VnScriptSO : ScriptableObject
    {
        public string id;
        public string nextScene;
        public VnBeat[] beats;
    }
}
