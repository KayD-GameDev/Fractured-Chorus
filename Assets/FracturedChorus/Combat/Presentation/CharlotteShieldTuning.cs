using UnityEngine;

namespace FracturedChorus.Combat.Presentation
{
    public class CharlotteShieldTuning : MonoBehaviour
    {
        [Header("Skill 1 — Personal orbit shield")]
        [SerializeField] private float personalWorldSize = 3.54f;
        [SerializeField] private float personalHeightOffset = 0.39f;
        [SerializeField] private float personalOrbitRadius = 0.93f;

        [Header("Skill 3 — Dome ring")]
        [SerializeField] private float domeWorldSize = 2.9f;
        [SerializeField] private float domeXOffset = 0f;
        [SerializeField] private float domeHeightOffset = 0.75f;

        [Header("Counter shield (legacy)")]
        [SerializeField] private float worldSize = 3.8f;
        [SerializeField] private float forwardOffset = 1.35f;
        [SerializeField] private float heightOffset = 0.95f;

        public float PersonalWorldSize => Mathf.Max(0.2f, personalWorldSize);
        public float PersonalHeightOffset => personalHeightOffset;
        public float PersonalOrbitRadius => Mathf.Max(0.35f, personalOrbitRadius);
        public float DomeWorldSize => Mathf.Max(0.2f, domeWorldSize);
        public float DomeXOffset => domeXOffset;
        public float DomeHeightOffset => domeHeightOffset;
        public float WorldSize => Mathf.Max(0.2f, worldSize);
        public float ForwardOffset => forwardOffset;
        public float HeightOffset => heightOffset;

        public static CharlotteShieldTuning Resolve()
        {
            return FindAnyObjectByType<CharlotteShieldTuning>();
        }

        public void ApplyPersonal(float size, float height, float orbit)
        {
            personalWorldSize = Mathf.Max(0.2f, size);
            personalHeightOffset = height;
            personalOrbitRadius = Mathf.Max(0.35f, orbit);
        }

        public void ApplyDome(float size, float x, float height)
        {
            domeWorldSize = Mathf.Max(0.2f, size);
            domeXOffset = x;
            domeHeightOffset = height;
        }

        public void Apply(float size, float forward, float height)
        {
            worldSize = Mathf.Max(0.2f, size);
            forwardOffset = forward;
            heightOffset = height;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            personalWorldSize = Mathf.Max(0.2f, personalWorldSize);
            personalOrbitRadius = Mathf.Max(0.35f, personalOrbitRadius);
            domeWorldSize = Mathf.Max(0.2f, domeWorldSize);
            worldSize = Mathf.Max(0.2f, worldSize);
        }
#endif
    }
}
