using System.Collections;
using UnityEngine;

namespace FracturedChorus.Combat.Presentation
{
    public sealed class SkillVfxAttachView : MonoBehaviour
    {
        public static SkillVfxAttachView Spawn(
            Vector3 world,
            BossSwordShotSettings settings,
            Transform parent)
        {
            if (settings?.Sword == null)
            {
                return null;
            }

            var go = new GameObject("SkillVfxAttach");
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            go.transform.position = world;
            var view = go.AddComponent<SkillVfxAttachView>();
            view.StartCoroutine(view.PlayRoutine(settings));
            return view;
        }

        private IEnumerator PlayRoutine(BossSwordShotSettings settings)
        {
            var sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = settings.Sword;
            sr.sortingOrder = settings.SortingOrder;
            sr.color = Color.white;
            if (settings.ProjectileAdditive && settings.AdditiveMaterial != null)
            {
                sr.sharedMaterial = settings.AdditiveMaterial;
            }

            SkillVfxShotView.FitSpriteToWorldSize(sr, settings.SwordWorldLength);

            var seconds = Mathf.Max(0.01f, settings.ImpactSeconds);
            var elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.deltaTime;
                var u = Mathf.Clamp01(elapsed / seconds);
                var c = sr.color;
                sr.color = new Color(c.r, c.g, c.b, 1f - u);
                yield return null;
            }

            settings.OnImpact?.Invoke();
            Destroy(gameObject);
        }
    }
}
