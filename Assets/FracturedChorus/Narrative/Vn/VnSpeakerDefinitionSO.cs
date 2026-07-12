using System;
using UnityEngine;

namespace FracturedChorus.Narrative.Vn
{
    [Serializable]
    public sealed class VnExpressionSprite
    {
        public string expressionId;
        public Sprite sprite;
    }

    [CreateAssetMenu(
        fileName = "Speaker_",
        menuName = "Fractured Chorus/Narrative/VN Speaker Definition")]
    public sealed class VnSpeakerDefinitionSO : ScriptableObject
    {
        public string speakerId;
        public string displayName;
        public Sprite bustSprite;
        public VnExpressionSprite[] expressionSprites;
        public Color shadowColor = new Color(0.05f, 0.12f, 0.35f, 0.92f);
        public Vector2 shadowOffsetPixels = new Vector2(-18f, 14f);
        public bool facesRight;

        public Sprite ResolveBust(string expressionId)
        {
            if (!string.IsNullOrWhiteSpace(expressionId) && expressionSprites != null)
            {
                for (var i = 0; i < expressionSprites.Length; i++)
                {
                    var entry = expressionSprites[i];
                    if (entry != null &&
                        entry.expressionId == expressionId &&
                        entry.sprite != null)
                    {
                        return entry.sprite;
                    }
                }
            }

            return bustSprite;
        }

        public bool IsProtagonist => speakerId == VnSpeakerIds.Ren;
    }
}
