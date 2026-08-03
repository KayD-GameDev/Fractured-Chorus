using System.Collections;
using System.Collections.Generic;
using FracturedChorus.Combat.Units;
using FracturedChorus.UI;
using UnityEngine;

namespace FracturedChorus.Combat.Presentation
{
    public class CharlotteVfxView : MonoBehaviour
    {
        private const string ResourceRoot = "VFX/Combat/Charlotte/";
        private const int SortingOrder = 42;
        private const float AuraHeightOffset = 0.35f;
        private const float FrontShieldHeightOffset = 0.45f;
        private const float FrontShieldForward = 1.15f;
        private const float ScatterHeightOffset = 0.45f;
        private const float ScatterWorldSize = 1.8f;
        private const float CounterShieldWorldSize = 2.325f;
        private const float AuraWorldSize = 2.175f;

        private static Sprite s_scatter;
        private static Sprite s_shieldCreate;
        private static Sprite s_counterShield;
        private static Material s_additiveMat;
        private static bool s_loaded;
        private static readonly Dictionary<CombatUnit, CharlotteVfxView> ActiveAuras = new();

        private CombatUnit _boundUnit;
        private SpriteRenderer _aura;
        private Vector3 _auraBaseScale;

        public static void SpawnNoteScatter(Vector3 impactPoint, Transform parent = null)
        {
            EnsureSprites();
            var go = CreateRoot("CharlotteNoteScatterVfx", parent);
            var view = go.AddComponent<CharlotteVfxView>();
            view.StartCoroutine(view.PlayScatterRoutine(impactPoint));
        }

        public static void SpawnCounterFrontShield(UnitView caster, Transform parent = null)
        {
            if (caster == null)
            {
                return;
            }

            EnsureSprites();
            var go = CreateRoot("CharlotteCounterFrontShieldVfx", parent);
            var view = go.AddComponent<CharlotteVfxView>();
            view.StartCoroutine(view.PlayFrontShieldRoutine(caster));
        }

        public static void EnsurePersistentAura(CombatUnit unit, UnitView caster, Transform parent = null)
        {
            if (unit == null || caster == null || unit.Shield <= 0)
            {
                return;
            }

            EnsureSprites();
            if (ActiveAuras.TryGetValue(unit, out var existing) && existing != null)
            {
                return;
            }

            var go = CreateRoot("CharlotteShieldAuraVfx", parent);
            var view = go.AddComponent<CharlotteVfxView>();
            view.BeginPersistentAura(unit, caster);
            ActiveAuras[unit] = view;
        }

        public static void ClearAura(CombatUnit unit)
        {
            if (unit == null)
            {
                return;
            }

            if (ActiveAuras.TryGetValue(unit, out var view) && view != null)
            {
                Destroy(view.gameObject);
            }

            ActiveAuras.Remove(unit);
        }

        private static GameObject CreateRoot(string name, Transform parent)
        {
            var go = new GameObject(name);
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            return go;
        }

        private IEnumerator PlayScatterRoutine(Vector3 impactPoint)
        {
            var pos = impactPoint + Vector3.up * ScatterHeightOffset;
            var scatter = CreateSprite(s_scatter, pos, ScatterWorldSize);
            var baseScale = scatter.transform.localScale;
            var elapsed = 0f;
            const float duration = 0.4f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var alpha = t < 0.6f ? 1f : 1f - (t - 0.6f) / 0.4f;
                SetScaleAlpha(scatter, baseScale, Mathf.Lerp(0.45f, 1.15f, EaseOut(t)), alpha);
                yield return null;
            }

            Destroy(gameObject);
        }

        private IEnumerator PlayFrontShieldRoutine(UnitView caster)
        {
            var facing = ResolvePlayerForward(caster);
            var feet = caster.FeetWorldPosition;
            var pos = feet + facing * FrontShieldForward + Vector3.up * FrontShieldHeightOffset;
            var shield = CreateSprite(s_counterShield, pos, CounterShieldWorldSize);
            var baseScale = shield.transform.localScale;

            var elapsed = 0f;
            const float duration = 0.55f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var grow = t < 0.3f ? EaseOut(t / 0.3f) : 1f;
                var alpha = t < 0.65f ? 1f : 1f - (t - 0.65f) / 0.35f;
                var follow = caster.FeetWorldPosition + facing * FrontShieldForward +
                             Vector3.up * FrontShieldHeightOffset;
                shield.transform.position = follow;
                SetScaleAlpha(shield, baseScale, Mathf.Lerp(0.5f, 1.05f, grow), alpha);
                yield return null;
            }

            Destroy(gameObject);
        }

        private void BeginPersistentAura(CombatUnit unit, UnitView caster)
        {
            _boundUnit = unit;
            unit.OnHpChanged += HandleBoundHpChanged;
            var pos = caster.FeetWorldPosition + Vector3.up * AuraHeightOffset;
            _aura = CreateSprite(s_shieldCreate, pos, AuraWorldSize);
            _auraBaseScale = _aura.transform.localScale;
            StartCoroutine(PersistAuraRoutine(caster));
        }

        private IEnumerator PersistAuraRoutine(UnitView caster)
        {
            var appear = 0f;
            while (appear < 0.25f)
            {
                appear += Time.deltaTime;
                var t = Mathf.Clamp01(appear / 0.25f);
                FollowAura(caster);
                SetScaleAlpha(_aura, _auraBaseScale, Mathf.Lerp(0.6f, 1f, EaseOut(t)), t);
                yield return null;
            }

            while (_boundUnit != null && _boundUnit.IsAlive && _boundUnit.Shield > 0)
            {
                FollowAura(caster);
                var pulse = 1f + 0.04f * Mathf.Sin(Time.time * 4f);
                SetScaleAlpha(_aura, _auraBaseScale, pulse, 0.9f);
                yield return null;
            }

            var fade = 0f;
            while (fade < 0.2f)
            {
                fade += Time.deltaTime;
                var t = Mathf.Clamp01(fade / 0.2f);
                FollowAura(caster);
                SetScaleAlpha(_aura, _auraBaseScale, 1f, 1f - t);
                yield return null;
            }

            if (_boundUnit != null)
            {
                ActiveAuras.Remove(_boundUnit);
            }

            Destroy(gameObject);
        }

        private void FollowAura(UnitView caster)
        {
            if (_aura == null || caster == null)
            {
                return;
            }

            _aura.transform.position = caster.FeetWorldPosition + Vector3.up * AuraHeightOffset;
        }

        private void HandleBoundHpChanged(CombatUnit unit)
        {
            if (unit == null || unit.Shield <= 0 || !unit.IsAlive)
            {
                // PersistAuraRoutine exits on next frame via Shield check.
            }
        }

        private void OnDestroy()
        {
            if (_boundUnit != null)
            {
                _boundUnit.OnHpChanged -= HandleBoundHpChanged;
                if (ActiveAuras.TryGetValue(_boundUnit, out var self) && self == this)
                {
                    ActiveAuras.Remove(_boundUnit);
                }
            }
        }

        private static Vector3 ResolvePlayerForward(UnitView caster)
        {
            // Party bên trái, enemy bên phải → khiên phía trước = +X.
            return Vector3.right;
        }

        private SpriteRenderer CreateSprite(Sprite sprite, Vector3 worldPos, float worldHeight)
        {
            var child = new GameObject(sprite != null ? sprite.name : "Vfx");
            child.transform.SetParent(transform, false);
            child.transform.position = worldPos;
            var sr = child.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = SortingOrder;
            sr.color = Color.white;
            EnsureAdditiveMaterial();
            if (s_additiveMat != null)
            {
                sr.sharedMaterial = s_additiveMat;
            }

            if (sprite != null)
            {
                var h = Mathf.Max(0.001f, sprite.bounds.size.y);
                var scale = worldHeight / h;
                child.transform.localScale = new Vector3(scale, scale, 1f);
            }

            return sr;
        }

        private static void SetScaleAlpha(SpriteRenderer sr, Vector3 baseScale, float mul, float alpha)
        {
            if (sr == null)
            {
                return;
            }

            sr.transform.localScale = baseScale * mul;
            var c = sr.color;
            sr.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(alpha));
        }

        private static float EaseOut(float t) => 1f - (1f - t) * (1f - t);

        private static void EnsureSprites()
        {
            if (s_loaded)
            {
                return;
            }

            s_scatter = Resources.Load<Sprite>(ResourceRoot + "charlotte_vfx_note_scatter_v1");
            s_shieldCreate = Resources.Load<Sprite>(ResourceRoot + "charlotte_vfx_shield_create_v1");
            s_counterShield = Resources.Load<Sprite>(ResourceRoot + "charlotte_vfx_counter_shield_v1");
            EnsureAdditiveMaterial();
            s_loaded = true;
        }

        private static void EnsureAdditiveMaterial()
        {
            if (s_additiveMat != null)
            {
                return;
            }

            var shader = Shader.Find("FracturedChorus/VFX/RenBulletAdditive")
                         ?? Shader.Find("Sprites/Default");
            if (shader == null)
            {
                return;
            }

            s_additiveMat = new Material(shader)
            {
                name = "CharlotteVfxAdditive",
                hideFlags = HideFlags.HideAndDontSave
            };
        }
    }
}
