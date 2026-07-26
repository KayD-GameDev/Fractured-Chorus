using UnityEngine;

namespace FracturedChorus.UI
{
    public enum BossNoteNumberRole
    {
        Single = 0,
        BeamedLeft = 1,
        BeamedRight = 2
    }

    public sealed class BossNoteNumberHandle : MonoBehaviour
    {
        public BossNoteNumberRole Role;
        public int VariantIndex;
        public Vector2 BaseLocalPos;
        public RectTransform Rect => transform as RectTransform;
    }
}
