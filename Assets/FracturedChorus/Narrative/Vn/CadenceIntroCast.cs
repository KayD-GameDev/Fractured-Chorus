using System.Collections.Generic;
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
            "Assets/FracturedChorus/Art/Characters/Coda/Cadence/coda_cadence_bust_upper_v1.png";
        private const string KikiCadenceAsset =
            "Assets/FracturedChorus/Art/Characters/Kiki/VnBust/kiki_cadence_bust_neutral_v1.png";
        private const string CharlotteBustFolder =
            "Assets/FracturedChorus/Art/Characters/Charlotte/VnBust/";

        public static void Install()
        {
            VnRuntimeSpeakers.Clear();
            VnRuntimeSpeakers.Set(Create(VnSpeakerIds.Ren, "Ren", "ren_school_bust_neutral_v1", RenSchoolAsset,
                new Color(0.05f, 0.12f, 0.35f, 0.92f)));
            VnRuntimeSpeakers.Set(Create(VnSpeakerIds.Coda, "Coda", "coda_cadence_bust_upper_v1", CodaCadenceAsset,
                new Color(0.45f, 0.55f, 0.85f, 0.92f)));
            VnRuntimeSpeakers.Set(Create(VnSpeakerIds.Kiki, "Kiki", "kiki_cadence_bust_neutral_v1", KikiCadenceAsset,
                new Color(0.45f, 0.12f, 0.28f, 0.92f), 1.2f));
            InstallCharlotte();
        }

        private static void InstallCharlotte()
        {
            var neutral = Load("charlotte_bust_neutral_v1", CharlotteBustFolder + "charlotte_bust_neutral_v1.png");
            var startled = Load("charlotte_bust_startled_v1", CharlotteBustFolder + "charlotte_bust_startled_v1.png");
            var grim = Load("charlotte_bust_grim_v1", CharlotteBustFolder + "charlotte_bust_grim_v1.png");
            var bust = neutral != null ? neutral : grim != null ? grim : startled;
            if (bust == null)
            {
                return;
            }

            var speaker = ScriptableObject.CreateInstance<VnSpeakerDefinitionSO>();
            speaker.speakerId = VnSpeakerIds.Charlotte;
            speaker.displayName = "Charlotte";
            speaker.bustSprite = bust;
            speaker.portraitScale = 1f;
            speaker.shadowColor = new Color(0.28f, 0.08f, 0.06f, 0.9f);
            speaker.shadowOffsetPixels = VnDialoguePortraitLayout.DefaultShadowOffset;
            var expressions = new List<VnExpressionSprite>(3);
            AddExpression(expressions, "neutral", neutral);
            AddExpression(expressions, "startled", startled);
            AddExpression(expressions, "grim", grim);
            speaker.expressionSprites = expressions.ToArray();
            VnRuntimeSpeakers.Set(speaker);
        }

        private static void AddExpression(List<VnExpressionSprite> expressions, string expressionId, Sprite sprite)
        {
            if (sprite == null)
            {
                return;
            }

            expressions.Add(new VnExpressionSprite
            {
                expressionId = expressionId,
                sprite = sprite
            });
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

            var representations = AssetDatabase.LoadAllAssetRepresentationsAtPath(assetPath);
            for (var i = 0; i < representations.Length; i++)
            {
                if (representations[i] is Sprite sprite)
                {
                    return sprite;
                }
            }
#endif
            return Resources.Load<Sprite>("VN/Portraits/" + resourceName);
        }
    }
}
