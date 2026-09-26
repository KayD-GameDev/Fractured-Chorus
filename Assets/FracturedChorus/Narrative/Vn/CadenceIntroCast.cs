using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FracturedChorus.Narrative.Vn
{
    public static class CadenceIntroCast
    {
        private const string RenSchoolAsset =
            "Assets/FracturedChorus/Art/UI/Narrative/Portraits/ren_school_bust_neutral_v1.png";
        private const string CodaCadenceAsset =
            "Assets/FracturedChorus/Art/Characters/Coda/Chibi/coda_cadence_chibi_bust_v1.png";
        private const string KikiCadenceAsset =
            "Assets/FracturedChorus/Art/Characters/Kiki/VnBust/kiki_cadence_bust_neutral_v1.png";

        public static void Install()
        {
            VnRuntimeSpeakers.Clear();
            VnRuntimeSpeakers.Set(Create(VnSpeakerIds.Ren, "Ren", "ren_school_bust_neutral_v1", RenSchoolAsset,
                new Color(0.05f, 0.12f, 0.35f, 0.92f)));
            VnRuntimeSpeakers.Set(Create(VnSpeakerIds.Coda, "Coda", "coda_cadence_bust_neutral_v1", CodaCadenceAsset,
                new Color(0.45f, 0.55f, 0.85f, 0.92f)));
            VnRuntimeSpeakers.Set(Create(VnSpeakerIds.Kiki, "Kiki", "kiki_cadence_bust_neutral_v1", KikiCadenceAsset,
                new Color(0.45f, 0.12f, 0.28f, 0.92f), 1.2f));
        }

        private static VnSpeakerDefinitionSO Create(
            string speakerId,
            string displayName,
            string resourceName,
            string assetPath,
            Color shadow,
            float portraitScale = 1f)
        {
            var speaker = ScriptableObject.CreateInstance<VnSpeakerDefinitionSO>();
            speaker.speakerId = speakerId;
            speaker.displayName = displayName;
            speaker.bustSprite = Load(resourceName, assetPath);
            speaker.portraitScale = portraitScale;
            speaker.shadowColor = shadow;
            speaker.shadowOffsetPixels = VnDialoguePortraitLayout.DefaultShadowOffset;
            return speaker;
        }

        private static Sprite Load(string resourceName, string assetPath)
        {
#if UNITY_EDITOR
            var fromAsset = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (fromAsset != null)
            {
                return fromAsset;
            }
#endif
            return Resources.Load<Sprite>("VN/Portraits/" + resourceName);
        }
    }
}
