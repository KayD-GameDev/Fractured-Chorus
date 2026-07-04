using UnityEngine;

namespace FracturedChorus.Narrative
{
    [CreateAssetMenu(
        fileName = "PrologueVNLayoutConfig",
        menuName = "Fractured Chorus/Prologue VN Layout Config")]
    public class PrologueVNLayoutConfig : ScriptableObject
    {
        [Header("Contract — anchors on ContractPaper (0–1)")]
        public Vector2 nameLineMin = new Vector2(0.29f, 0.235f);
        public Vector2 nameLineMax = new Vector2(0.83f, 0.285f);
        public Vector2 signatureLineMin = new Vector2(0.29f, 0.125f);
        public Vector2 signatureLineMax = new Vector2(0.83f, 0.175f);

        public void CaptureFrom(RectTransform paper, RectTransform nameField, RectTransform signatureField)
        {
            if (nameField != null)
            {
                PrologueContractLayout.CaptureFieldAnchors(paper, nameField, out nameLineMin, out nameLineMax);
            }

            if (signatureField != null)
            {
                PrologueContractLayout.CaptureFieldAnchors(
                    paper,
                    signatureField,
                    out signatureLineMin,
                    out signatureLineMax);
            }
        }
    }
}
