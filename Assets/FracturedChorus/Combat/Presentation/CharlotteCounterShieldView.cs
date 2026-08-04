using System.Collections;
using System.Collections.Generic;
using FracturedChorus.Combat.Units;
using FracturedChorus.UI;
using UnityEngine;

namespace FracturedChorus.Combat.Presentation
{
    public sealed class CharlotteCounterShieldSettings
    {
        public Sprite Shield;
        public Sprite CreateBurst;
        public Material AdditiveMaterial;
        public float WorldSize = 3.8f;
        public float ForwardOffset = 1.35f;
        public float HeightOffset = 0.95f;
        public float RiseSeconds = 0.16f;
        public float MaxHoldSeconds = 2.5f;
        public float FadeSeconds = 0.16f;
        public int SortingOrder = 42;
        public Color Tint = new Color(1f, 0.82f, 0.28f, 1f);
    }

    public class CharlotteCounterShieldView : MonoBehaviour
    {
        private const string ResourceRoot = "VFX/Combat/Charlotte/";
        private static readonly List<CharlotteCounterShieldView> Active = new();

        private CharlotteCounterShieldSettings _settings;
        private SpriteRenderer _shield;
        private SpriteRenderer _burst;
        private Transform _follow;
        private Vector3 _localOffset;
        private float _forwardSign = 1f;
        private bool _dismissRequested;
        private bool _finished;

        public static bool IsCharlotteUnit(CombatUnit unit, UnitView view = null)
        {
            if (unit != null)
            {
                var id = unit.UnitId ?? string.Empty;
                if (id.IndexOf("charlotte", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || id.IndexOf("charlott", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || id.IndexOf("tank", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }

                if (unit.Role == UnitRole.Tank)
                {
                    return true;
                }
            }

            if (view == null)
            {
                return false;
            }

            var key = view.DemoUnitKey ?? string.Empty;
            return key.IndexOf("charlotte", System.StringComparison.OrdinalIgnoreCase) >= 0
                   || key.IndexOf("charlott", System.StringComparison.OrdinalIgnoreCase) >= 0
                   || key.IndexOf("tank", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static CharlotteCounterShieldView TrySpawnFor(
            UnitView charlotteView,
            Vector3? faceTowardWorld = null,
            float maxHoldSeconds = -1f,
            Transform parent = null)
        {
            if (charlotteView == null || !IsCharlotteUnit(charlotteView.Unit, charlotteView))
            {
                return null;
            }

            var settings = BuildDefaultSettings();
            if (maxHoldSeconds > 0f)
            {
                settings.MaxHoldSeconds = maxHoldSeconds;
            }

            if (settings.Shield == null && settings.CreateBurst == null)
            {
                return null;
            }

            var go = new GameObject("CharlotteCounterShield");
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            var view = go.AddComponent<CharlotteCounterShieldView>();
            view._settings = settings;
            view._follow = charlotteView.transform;
            view._forwardSign = ResolveForwardSign(charlotteView, faceTowardWorld);
            var feet = charlotteView.FeetWorldPosition;
            var world = feet
                        + Vector3.right * (settings.ForwardOffset * view._forwardSign)
                        + Vector3.up * settings.HeightOffset;
            view._localOffset = world - charlotteView.transform.position;
            view.transform.position = world;
            Active.Add(view);
            view.Build();
            view.StartCoroutine(view.PlayRoutine());
            return view;
        }

        public static IEnumerator DismissAllAndWait()
        {
            if (Active.Count == 0)
            {
                yield break;
            }

            var snapshot = Active.ToArray();
            foreach (var shield in snapshot)
            {
                if (shield != null)
                {
                    shield.RequestDismiss();
                }
            }

            var timeout = 1.2f;
            while (timeout > 0f)
            {
                var any = false;
                for (var i = 0; i < Active.Count; i++)
                {
                    if (Active[i] != null && !Active[i]._finished)
                    {
                        any = true;
                        break;
                    }
                }

                if (!any)
                {
                    yield break;
                }

                timeout -= Time.deltaTime;
                yield return null;
            }
        }

        public void RequestDismiss()
        {
            _dismissRequested = true;
        }

        private static float ResolveForwardSign(UnitView charlotteView, Vector3? faceTowardWorld)
        {
            if (faceTowardWorld.HasValue)
            {
                var delta = faceTowardWorld.Value.x - charlotteView.FeetWorldPosition.x;
                if (Mathf.Abs(delta) > 0.05f)
                {
                    return Mathf.Sign(delta);
                }
            }

            return 1f;
        }

        private static CharlotteCounterShieldSettings BuildDefaultSettings()
        {
            var tuning = CharlotteShieldTuning.Resolve();
            return new CharlotteCounterShieldSettings
            {
                Shield = LoadSprite("charlotte_vfx_rho_shield_hold_2layer_v3")
                         ?? LoadSprite("charlotte_vfx_rho_shield_hold_v2")
                         ?? LoadSprite("charlotte_vfx_counter_shield_v1"),
                CreateBurst = LoadSprite("charlotte_vfx_rho_shield_impact_2layer_v3")
                              ?? LoadSprite("charlotte_vfx_rho_shield_impact_v2")
                              ?? LoadSprite("charlotte_vfx_shield_create_v1"),
                AdditiveMaterial = ResolveAdditiveMaterial(),
                WorldSize = tuning != null ? tuning.WorldSize : 3.8f,
                ForwardOffset = tuning != null ? tuning.ForwardOffset : 1.35f,
                HeightOffset = tuning != null ? tuning.HeightOffset : 0.95f
            };
        }

        private static Sprite LoadSprite(string fileName)
        {
            var path = ResourceRoot + fileName;
            var sprite = Resources.Load<Sprite>(path);
            if (sprite != null)
            {
                return sprite;
            }

            var sprites = Resources.LoadAll<Sprite>(path);
            if (sprites != null && sprites.Length > 0)
            {
                return sprites[0];
            }

            var tex = Resources.Load<Texture2D>(path);
            if (tex == null)
            {
                return null;
            }

            return Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                100f);
        }

        private static Material ResolveAdditiveMaterial()
        {
            var shader = Shader.Find("FracturedChorus/VFX/RenBulletAdditive")
                         ?? Shader.Find("Sprites/Default");
            if (shader == null)
            {
                return null;
            }

            return new Material(shader)
            {
                name = "CharlotteShieldAdditive_Runtime",
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        private void Build()
        {
            if (_settings.CreateBurst != null)
            {
                _burst = CreateRenderer("CreateBurst", _settings.CreateBurst, _settings.SortingOrder + 1);
                FitSprite(_burst, _settings.WorldSize * 0.55f);
                _burst.color = new Color(_settings.Tint.r, _settings.Tint.g, _settings.Tint.b, 0f);
            }

            if (_settings.Shield != null)
            {
                _shield = CreateRenderer("Shield", _settings.Shield, _settings.SortingOrder);
                FitSprite(_shield, _settings.WorldSize * 0.2f);
                _shield.color = new Color(_settings.Tint.r, _settings.Tint.g, _settings.Tint.b, 0f);
                ApplyFacingScale(_shield);
            }
        }

        private SpriteRenderer CreateRenderer(string name, Sprite sprite, int order)
        {
            var child = new GameObject(name);
            child.transform.SetParent(transform, false);
            var sr = child.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            if (_settings.AdditiveMaterial != null)
            {
                sr.sharedMaterial = _settings.AdditiveMaterial;
            }

            return sr;
        }

        private void FitSprite(SpriteRenderer sr, float worldSize)
        {
            if (sr == null || sr.sprite == null)
            {
                return;
            }

            var native = Mathf.Max(sr.sprite.bounds.size.x, sr.sprite.bounds.size.y);
            var scale = native > 0.001f ? worldSize / native : 1f;
            sr.transform.localScale = new Vector3(scale, scale, 1f);
            ApplyFacingScale(sr);
        }

        private void ApplyFacingScale(SpriteRenderer sr)
        {
            if (sr == null)
            {
                return;
            }

            var s = sr.transform.localScale;
            var magX = Mathf.Abs(s.x);
            if (magX < 0.0001f)
            {
                magX = 1f;
            }

            // Art faces left by default; flip when facing boss to the right.
            var faceBoss = _forwardSign >= 0f ? -1f : 1f;
            sr.transform.localScale = new Vector3(magX * faceBoss, Mathf.Abs(s.y), 1f);
        }

        private void LateUpdate()
        {
            if (_follow != null)
            {
                transform.position = _follow.position + _localOffset;
            }
        }

        private IEnumerator PlayRoutine()
        {
            var rise = Mathf.Max(0.05f, _settings.RiseSeconds);
            var maxHold = Mathf.Max(0.05f, _settings.MaxHoldSeconds);
            var fade = Mathf.Max(0.05f, _settings.FadeSeconds);
            var elapsed = 0f;

            while (elapsed < rise)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / rise);
                ApplyRise(t * t * (3f - 2f * t));
                yield return null;
            }

            ApplyRise(1f);

            if (_burst != null)
            {
                var burstElapsed = 0f;
                const float burstLife = 0.22f;
                while (burstElapsed < burstLife && !_dismissRequested)
                {
                    burstElapsed += Time.deltaTime;
                    var u = Mathf.Clamp01(burstElapsed / burstLife);
                    FitSprite(_burst, _settings.WorldSize * Mathf.Lerp(0.7f, 1.15f, u));
                    var c = _settings.Tint;
                    _burst.color = new Color(c.r, c.g, c.b, Mathf.Lerp(0.95f, 0f, u));
                    yield return null;
                }

                if (_burst != null)
                {
                    _burst.enabled = false;
                }
            }

            var holdElapsed = 0f;
            while (!_dismissRequested && holdElapsed < maxHold)
            {
                holdElapsed += Time.deltaTime;
                var pulse = 1f + Mathf.Sin(holdElapsed * 8f) * 0.03f;
                if (_shield != null)
                {
                    FitSprite(_shield, _settings.WorldSize * pulse);
                    var c = _settings.Tint;
                    var flicker = 0.78f + 0.22f * Mathf.Abs(Mathf.Sin(holdElapsed * 5.5f));
                    _shield.color = new Color(c.r, c.g, c.b, flicker);
                }

                yield return null;
            }

            elapsed = 0f;
            var shieldA = _shield != null ? _shield.color.a : 0f;
            while (elapsed < fade)
            {
                elapsed += Time.deltaTime;
                var u = 1f - Mathf.Clamp01(elapsed / fade);
                if (_shield != null)
                {
                    var c = _shield.color;
                    _shield.color = new Color(c.r, c.g, c.b, shieldA * u);
                    FitSprite(_shield, _settings.WorldSize * Mathf.Lerp(0.85f, 1f, u));
                }

                yield return null;
            }

            _finished = true;
            Active.Remove(this);
            Destroy(gameObject);
        }

        private void ApplyRise(float t)
        {
            if (_shield != null)
            {
                FitSprite(_shield, _settings.WorldSize * Mathf.Lerp(0.25f, 1f, t));
                var c = _settings.Tint;
                _shield.color = new Color(c.r, c.g, c.b, Mathf.Lerp(0f, 0.95f, t));
                _shield.transform.localPosition = Vector3.up * Mathf.Lerp(-0.35f, 0f, t);
            }

            if (_burst != null)
            {
                FitSprite(_burst, _settings.WorldSize * Mathf.Lerp(0.35f, 0.75f, t));
                var c = _settings.Tint;
                _burst.color = new Color(c.r, c.g, c.b, Mathf.Lerp(0f, 0.9f, t));
            }
        }

        private void OnDestroy()
        {
            _finished = true;
            Active.Remove(this);
        }
    }
}
