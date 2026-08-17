using UnityEngine;

namespace FracturedChorus.Data
{
    [CreateAssetMenu(fileName = "RunMapPlayerMarkerConfig", menuName = "Fractured Chorus/Run Map Player Marker")]
    public sealed class RunMapPlayerMarkerConfigSO : ScriptableObject
    {
        [Header("Sprites")]
        [SerializeField] private Sprite idleSprite;
        [SerializeField] private Sprite travelSprite;

        [Header("Placement")]
        [SerializeField] private Vector2 markerSize = new Vector2(53f, 73f);
        [SerializeField] private Vector2 footOffset = Vector2.zero;

        [Header("Travel jump")]
        [SerializeField] private float jumpHeight = 50f;
        [SerializeField] private float travelDuration = 0.35f;
        [SerializeField] private float spinTurns = 1f;
        [SerializeField] private float travelScale = 1.08f;

        public Sprite IdleSprite => idleSprite;
        public Sprite TravelSprite => travelSprite != null ? travelSprite : idleSprite;
        public Vector2 MarkerSize => markerSize;
        public Vector2 FootOffset => footOffset;
        public float JumpHeight => jumpHeight;
        public float TravelDuration => Mathf.Max(0.08f, travelDuration);
        public float SpinTurns => spinTurns;
        public float TravelScale => travelScale;
    }
}