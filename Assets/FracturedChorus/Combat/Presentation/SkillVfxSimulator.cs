using System.Collections.Generic;
using FracturedChorus.Combat.Grid;
using FracturedChorus.Data;
using FracturedChorus.UI;
using UnityEngine;

namespace FracturedChorus.Combat.Presentation
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class SkillVfxSimulator : MonoBehaviour
    {
        public const string PreviewName = "SkillVfxSimulator";
        public const string PreviewRootName = "_VfxPreview";
        public const string ImpactChildName = "VfxImpact";

        [Header("Skill")]
        [SerializeField] private SkillDefinitionSO skill;
        [SerializeField] private SkillVfxProfileSO profileOverride;

        [Header("Hierarchy")]
        [SerializeField] private UnitView caster;
        [SerializeField] private UnitProjectileAnchor projectileA;
        [SerializeField] private List<UnitView> targets = new();

        [Header("Preview")]
        [SerializeField] private bool previewLive;
        [SerializeField] private bool previewPaused;
        [SerializeField] private bool hideSprite;
        [SerializeField] private bool hidePattern;
        [SerializeField] private bool loopSlide = true;
        [SerializeField] [Range(0.2f, 4f)] private float previewSpeed = 1f;
        [SerializeField] private bool showImpact = true;

        private readonly List<UnitView> _opponents = new();
        private readonly List<SpriteRenderer> _shots = new();
        private readonly List<SpriteRenderer> _impacts = new();
        private SpriteRenderer _impact;
        private float _animTime;
        private float _previewWorldSize;
        private SkillVfxProfileSO _boundProfile;
        private Vector3 _fromOffset;
        private Vector3 _toOffset;
        private Vector3 _projectileEuler;
        private bool _poseApplied;
        private bool _editorRefreshQueued;
        private bool _canMutatePreview;
        private bool _arrivalFired;

        public SkillDefinitionSO Skill => skill;
        public SkillVfxProfileSO Profile => profileOverride != null
            ? profileOverride
            : skill != null ? skill.vfxProfile : null;

        public UnitView Caster => caster;
        public UnitProjectileAnchor ProjectileA => projectileA;
        public IReadOnlyList<UnitView> AssignedTargets => targets;
        public bool PreviewLive => previewLive;
        public bool PreviewPaused => previewPaused;
        public bool IsPlaying => previewLive && !previewPaused;
        public bool ShowSprite => !hideSprite;
        public bool ShowImpact => showImpact;
        public bool ShowPattern => !hidePattern;
        public Vector3 FromWorld { get; private set; }
        public Vector3 ToWorld { get; private set; }
        public Vector3 FromOffset => _fromOffset;
        public Vector3 ToOffset => _toOffset;
        public Vector3 ProjectileEuler => _projectileEuler;
        public IReadOnlyList<UnitView> Opponents => _opponents;
        public bool ArrivedAtB { get; private set; }

        public float CurrentProjectileWorldSize
        {
            get
            {
                var profile = Profile;
                if (profile != null && profile.kind == SkillVfxKind.Hit)
                {
                    var impactSize = MeasureWorldSize(_impact);
                    return impactSize > 0.05f ? impactSize : profile.impactWorldSize;
                }

                return _previewWorldSize > 0.05f ? _previewWorldSize : MeasureWorldSize(PrimaryShot);
            }
        }

        public string PoseBindingLabel
        {
            get
            {
                var state = UnitView.ResolveAttackVisualState(skill);
                var profile = Profile;
                var profileName = profile != null ? profile.name : "(no profile)";
                var clipName = ResolvePoseClipName(state);
                return $"VFX {profileName} ↔ {state} / {clipName}";
            }
        }

        private SpriteRenderer PrimaryShot => _shots.Count > 0 ? _shots[0] : null;

        public static SkillVfxSimulator EnsureOn(UnitView view)
        {
            if (view == null)
            {
                return null;
            }

            var sim = view.GetComponent<SkillVfxSimulator>();
            if (sim == null)
            {
                sim = view.gameObject.AddComponent<SkillVfxSimulator>();
            }

            sim.BindToHostUnit();
            sim.EnsureOppositeTargets();
            view.EnsureProjectile();
            return sim;
        }

        public void BindToHostUnit()
        {
            var view = GetComponent<UnitView>();
            if (view == null)
            {
                return;
            }

            caster = view;
            view.EnsureProjectile();
            if (projectileA == null)
            {
                projectileA = view.Projectile;
            }

            if (skill == null)
            {
                skill = ResolveSkillFromUnit(view);
            }

            EnsureOppositeTargets();
            RefreshOpponents();
        }

        public void AssignCaster(UnitView view)
        {
            caster = view;
            if (view == null)
            {
                return;
            }

            view.EnsureProjectile();
            projectileA = view.Projectile;
            EnsureOppositeTargets();
            RefreshOpponents();
        }

        public void AssignProjectileA(UnitProjectileAnchor anchor)
        {
            projectileA = anchor;
            if (anchor == null)
            {
                return;
            }

            var view = anchor.GetComponentInParent<UnitView>();
            if (view != null && GetComponent<UnitView>() == null)
            {
                caster = view;
            }

            RefreshOpponents();
        }

        public void FillOppositeTargets()
        {
            targets.Clear();
            if (caster == null)
            {
                return;
            }

            var views = FindObjectsByType<UnitView>(FindObjectsInactive.Exclude);
            for (var i = 0; i < views.Length; i++)
            {
                var other = views[i];
                if (other == null || other == caster || other.Side == caster.Side)
                {
                    continue;
                }

                other.EnsureReceiveDmg();
                targets.Add(other);
            }

            RefreshOpponents();
        }

        public void SetPreviewLive(bool live)
        {
            previewLive = live;
            if (previewLive)
            {
                BindToHostUnit();
                EnsureOppositeTargets();
                ApplyCasterAttackPose();
                RefreshVisual(true, preferSceneScale: true);
                return;
            }

            previewPaused = false;
            _animTime = 0f;
            _arrivalFired = false;
            ArrivedAtB = false;
            RestoreCasterIdle();
            if (ShowSprite || ShowPattern)
            {
                RefreshVisual(false);
                return;
            }

            DestroyPreviewRoot();
        }

        public void SetPreviewPaused(bool paused)
        {
            previewPaused = paused;
            RefreshVisual(false);
        }

        public void Play()
        {
            var resume = previewLive && previewPaused;
            if (!resume)
            {
                _animTime = 0f;
            }

            previewPaused = false;
            _arrivalFired = false;
            ArrivedAtB = false;
            SetPreviewLive(true);
        }

        public void Pause()
        {
            if (!previewLive)
            {
                return;
            }

            SetPreviewPaused(true);
        }

        public void TogglePlayPause()
        {
            if (IsPlaying)
            {
                Pause();
                return;
            }

            Play();
        }

        public void SetShowSprite(bool show)
        {
            hideSprite = !show;
            if (!show && !ShowPattern && !ShowImpact && !previewLive)
            {
                DestroyPreviewRoot();
                return;
            }

            RefreshVisual(true);
        }

        public void SetShowImpact(bool show)
        {
            showImpact = show;
            if (!show)
            {
                DestroyAllImpacts();
                if (!ShowSprite && !ShowPattern && !previewLive)
                {
                    DestroyPreviewRoot();
                    return;
                }
            }

            RefreshVisual(true);
        }

        public void SetShowPattern(bool show)
        {
            hidePattern = !show;
            RefreshOpponents();
            ApplyLayout(Profile);
        }

        public void Bind(SkillDefinitionSO skillDef, UnitView casterView, UnitView targetView)
        {
            skill = skillDef;
            AssignCaster(casterView);
            if (targetView != null)
            {
                targets.RemoveAll(item => item == null);
                if (!targets.Contains(targetView))
                {
                    targets.Insert(0, targetView);
                }
            }

            RefreshOpponents();
            RefreshVisual(true);
        }

        public void SetSkill(SkillDefinitionSO skillDef)
        {
            skill = skillDef;
            profileOverride = null;
            _animTime = 0f;
            _boundProfile = null;
            if (previewLive)
            {
                ApplyCasterAttackPose();
                RefreshVisual(true, preferSceneScale: true);
                return;
            }

            RefreshVisual(true);
        }

        public SkillDefinitionSO[] CollectPreviewSkills()
        {
            var list = new List<SkillDefinitionSO>();
            AddUniqueSkills(list, caster != null ? caster.ResolvePreset()?.skills : null);
            if (list.Count == 0 && skill != null)
            {
                list.Add(skill);
            }

            return list.ToArray();
        }

        public int CurrentSkillTabIndex
        {
            get
            {
                var tabs = CollectPreviewSkills();
                for (var i = 0; i < tabs.Length; i++)
                {
                    if (tabs[i] == skill)
                    {
                        return i;
                    }
                }

                return 0;
            }
        }

        public static string SkillTabLabel(SkillDefinitionSO skillDef)
        {
            if (skillDef == null)
            {
                return "—";
            }

            var slot = skillDef.slotKind switch
            {
                SkillSlotKind.BasicAttack => "Basic",
                SkillSlotKind.Skill => "Skill",
                SkillSlotKind.Ultimate => "Ult",
                SkillSlotKind.Guard => "Guard",
                _ => "Skill"
            };

            var name = !string.IsNullOrWhiteSpace(skillDef.displayName)
                ? skillDef.displayName
                : skillDef.skillId;
            return string.IsNullOrWhiteSpace(name) ? slot : $"{slot} · {name}";
        }

        private static void AddUniqueSkills(List<SkillDefinitionSO> list, SkillDefinitionSO[] source)
        {
            if (source == null)
            {
                return;
            }

            for (var i = 0; i < source.Length; i++)
            {
                var item = source[i];
                if (item == null || list.Contains(item))
                {
                    continue;
                }

                list.Add(item);
            }
        }

        public void SetReceiveDmgWorld(UnitView targetView, Vector3 world)
        {
            SetVfxToWorld(targetView, world);
        }

        public void SetProjectileWorld(UnitView casterView, Vector3 world)
        {
            SetVfxFromWorld(world);
        }

        public void SetVfxFromWorld(Vector3 world)
        {
            _fromOffset = world - ResolveAnchorFrom();
            FromWorld = world;
            var profile = Profile;
            if (profile != null)
            {
                profile.fromOffset = _fromOffset;
            }

            RefreshVisual(false);
        }

        public void SetVfxToWorld(UnitView targetView, Vector3 world)
        {
            var anchor = targetView != null
                ? SkillVfxAnchorResolver.ResolveAnchorDestination(targetView)
                : ResolveAnchorFrom() + Vector3.left;
            _toOffset = world - anchor;
            ToWorld = world;
            var profile = Profile;
            if (profile != null)
            {
                profile.toOffset = _toOffset;
            }

            RefreshVisual(false);
        }

        public void SetProjectileEuler(Vector3 euler)
        {
            _projectileEuler = euler;
            var profile = Profile;
            if (profile != null)
            {
                profile.projectileEuler = euler;
                profile.facingOffsetDegrees = euler.z;
            }

            RefreshVisual(false);
        }

        public void SetProjectileWorldRotation(Quaternion world)
        {
            var e = world.eulerAngles;
            var profile = Profile;
            if (profile != null && profile.kind is SkillVfxKind.Projectile or SkillVfxKind.SpellBurst)
            {
                var travel = ResolveTravelAngle(ToWorld - FromWorld);
                e.z = Mathf.DeltaAngle(travel, e.z);
            }

            SetProjectileEuler(e);
        }

        public Quaternion CurrentProjectileWorldRotation
        {
            get
            {
                var shot = PrimaryShot;
                if (shot != null)
                {
                    return shot.transform.rotation;
                }

                return ResolveProjectileRotation(Profile, ToWorld - FromWorld);
            }
        }

        public void SetUniformProjectileScale(float worldSize)
        {
            var profile = Profile;
            if (profile == null)
            {
                return;
            }

            if (profile.kind == SkillVfxKind.Hit)
            {
                profile.impactWorldSize = Mathf.Max(0.05f, worldSize);
            }
            else
            {
                profile.projectileWorldSize = Mathf.Max(0.05f, worldSize);
                _previewWorldSize = profile.projectileWorldSize;
            }

            RefreshVisual(true);
        }

        public void CaptureScaleFromScene()
        {
            var profile = Profile;
            if (profile == null)
            {
                return;
            }

            if (profile.kind == SkillVfxKind.Hit)
            {
                var impactSize = MeasureWorldSize(_impact);
                if (impactSize > 0.05f)
                {
                    profile.impactWorldSize = impactSize;
                }

                return;
            }

            var measured = MeasureWorldSize(PrimaryShot);
            if (measured > 0.05f)
            {
                _previewWorldSize = measured;
                profile.projectileWorldSize = measured;
            }
        }

        public void CaptureRotationFromScene()
        {
            var shot = PrimaryShot;
            if (shot == null)
            {
                return;
            }

            var e = shot.transform.eulerAngles;
            var profile = Profile;
            if (profile != null && profile.kind is SkillVfxKind.Projectile or SkillVfxKind.SpellBurst)
            {
                e.z = Mathf.DeltaAngle(ResolveTravelAngle(ToWorld - FromWorld), e.z);
            }

            _projectileEuler = e;
        }

        public void SaveToProfile()
        {
            var profile = Profile;
            if (profile == null)
            {
                Debug.LogWarning("[SkillVfx] Không có SkillVfxProfileSO để Save.");
                return;
            }

            CaptureScaleFromScene();
            CaptureRotationFromScene();
            profile.ApplySavedLayout(
                profile.projectileWorldSize,
                profile.impactWorldSize,
                _fromOffset,
                _toOffset,
                _projectileEuler);

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(profile);
            if (skill != null)
            {
                skill.vfxProfile = profile;
                UnityEditor.EditorUtility.SetDirty(skill);
            }

            UnityEditor.EditorUtility.SetDirty(this);
            if (!Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
#endif
            Debug.Log(
                $"[SkillVfx] Saved {profile.name} kind={profile.kind} pattern={profile.HasPattern} " +
                $"size={profile.projectileWorldSize:F2} rot={_projectileEuler} from={_fromOffset} to={_toOffset}");
        }

        private void OnEnable()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.update -= EditorTick;
            UnityEditor.EditorApplication.update += EditorTick;
#endif
            BindToHostUnit();
            if (Application.isPlaying && !previewLive)
            {
                DestroyPreviewRoot();
                return;
            }

            if (previewLive)
            {
                ApplyCasterAttackPose();
            }

            if (ShouldPresentPreview)
            {
                RefreshVisualSafe(true, preferSceneScale: true);
                return;
            }

            DestroyPreviewRoot();
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.update -= EditorTick;
#endif
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (!previewLive)
            {
                if (transform.Find(PreviewRootName) != null)
                {
                    DestroyPreviewRoot();
                }

                return;
            }

            TickPreview(Time.deltaTime);
        }

#if UNITY_EDITOR
        private bool _wasSelectedPreview;

        private void EditorTick()
        {
            if (Application.isPlaying || this == null || !isActiveAndEnabled)
            {
                return;
            }

            var selected = IsSelectedPreview;
            if (!selected && !IsPlaying)
            {
                if (_wasSelectedPreview)
                {
                    HidePreviewRoot();
                    _wasSelectedPreview = false;
                }

                return;
            }

            if (selected && !_wasSelectedPreview)
            {
                _wasSelectedPreview = true;
                if (ShowSprite || ShowImpact || ShowPattern || previewLive)
                {
                    RefreshVisualSafe(true, preferSceneScale: true);
                }
            }

            if (!IsPlaying)
            {
                return;
            }

            TickPreview(0.016f);
            UnityEditor.SceneView.RepaintAll();
        }
#endif

        private void TickPreview(float deltaTime)
        {
            if (IsPlaying)
            {
                _animTime += deltaTime * Mathf.Max(0.05f, previewSpeed);
                TickCasterPose();
                RefreshVisualSafe(false);
                TickArrival(Profile);
            }
        }

        private void TickArrival(SkillVfxProfileSO profile)
        {
            if (profile == null || (!profile.Travels && profile.kind != SkillVfxKind.Hit))
            {
                return;
            }

            var travelT = ResolveTravelT(profile);
            if (profile.HasArrivedAtB(travelT))
            {
                if (_arrivalFired)
                {
                    return;
                }

                _arrivalFired = true;
                ArrivedAtB = true;
                return;
            }

            _arrivalFired = false;
            ArrivedAtB = false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_editorRefreshQueued)
            {
                return;
            }

            _editorRefreshQueued = true;
            UnityEditor.EditorApplication.delayCall += FlushEditorRefresh;
        }

        private void FlushEditorRefresh()
        {
            _editorRefreshQueued = false;
            if (this == null)
            {
                return;
            }

            BindToHostUnit();
            if (isActiveAndEnabled && ShouldPresentPreview)
            {
                RefreshVisualSafe(false);
            }
        }
#endif

        private bool ShouldPresentPreview
        {
            get
            {
                if (Application.isPlaying)
                {
                    return previewLive;
                }

                if (!ShowSprite && !ShowPattern && !ShowImpact && !previewLive)
                {
                    return false;
                }

                return IsSelectedPreview || IsPlaying;
            }
        }

        private bool IsSelectedPreview
        {
            get
            {
#if UNITY_EDITOR
                var selected = UnityEditor.Selection.activeGameObject;
                if (selected == null)
                {
                    return previewLive;
                }

                return selected == gameObject
                       || selected.transform.IsChildOf(transform)
                       || transform.IsChildOf(selected.transform);
#else
                return previewLive;
#endif
            }
        }

        public void RefreshVisual(bool forceRebuild)
        {
            RefreshVisualSafe(forceRebuild, preferSceneScale: false);
        }

        public void RefreshVisual(bool forceRebuild, bool preferSceneScale)
        {
            RefreshVisualSafe(forceRebuild, preferSceneScale);
        }

        private void RefreshVisualSafe(bool forceRebuild, bool preferSceneScale = false)
        {
            _canMutatePreview = true;
            try
            {
                RefreshVisualInternal(forceRebuild, preferSceneScale);
            }
            finally
            {
                _canMutatePreview = false;
            }
        }

        private void RefreshVisualInternal(bool forceRebuild, bool preferSceneScale)
        {
            RefreshOpponents();
            if (!ShouldPresentPreview)
            {
                HidePreviewRoot();
                return;
            }

            var profile = Profile;
            var createdProjectile = false;
            if (ShowSprite || ShowImpact)
            {
                createdProjectile = EnsureVisual(profile);
            }
            else
            {
                HidePreviewRenderers();
                DestroyAllImpacts();
            }

            if (ShowSprite && preferSceneScale && !createdProjectile)
            {
                var measured = MeasureWorldSize(PrimaryShot);
                if (measured > 0.001f)
                {
                    _previewWorldSize = measured;
                    _boundProfile = profile;
                }
                else
                {
                    SyncPreviewSize(profile, true);
                }
            }
            else if (profile != _boundProfile || _previewWorldSize < 0.05f)
            {
                SyncPreviewSize(profile, true);
            }

            ApplyLayout(profile);
        }

        private void SyncPreviewSize(SkillVfxProfileSO profile, bool forceRebuild)
        {
            if (profile == null)
            {
                return;
            }

            if (_boundProfile != profile)
            {
                _fromOffset = profile.fromOffset;
                _toOffset = profile.toOffset;
                _projectileEuler = ResolveLoadedEuler(profile);
            }

            if (forceRebuild || _boundProfile != profile || _previewWorldSize < 0.05f)
            {
                _previewWorldSize = Mathf.Max(0.05f, profile.projectileWorldSize);
                _boundProfile = profile;
            }
        }

        private bool EnsureVisual(SkillVfxProfileSO profile)
        {
            RefreshOpponents();
            caster?.EnsureProjectile();
            var previewRoot = EnsurePreviewRoot();
            if (previewRoot == null)
            {
                return false;
            }

            previewRoot.gameObject.SetActive((ShowSprite || ShowImpact) && ShouldPresentPreview);
            NeutralizePreviewRootScale(previewRoot);
            CleanupLegacyPreviewChildren(previewRoot);

            _shots.Clear();
            var created = false;
            var rebuild = false;
            var projectile = ResolvePreviewChild("Projectile", ref rebuild, out var madeShot);
            created |= madeShot;
            if (projectile != null)
            {
                var showShot = ShowSprite && profile != null && profile.projectileSprite != null;
                ApplyPreviewRenderer(
                    projectile,
                    showShot ? profile.projectileSprite : null,
                    showShot,
                    ResolvePreviewSorting(profile, 0));
                _shots.Add(projectile);
            }

            created |= EnsureImpactsOnTargets(profile);

            return created;
        }

        private bool EnsureImpactsOnTargets(SkillVfxProfileSO profile)
        {
            EnsureOppositeTargets();
            RefreshOpponents();
            var impactSprite = ResolveImpactSprite(profile);
            var show = ShowImpact && impactSprite != null && ShouldPresentPreview;
            if (!show)
            {
                DestroyAllImpacts();
                return false;
            }

            var created = false;
            _impacts.Clear();
            _impact = null;
            for (var i = 0; i < _opponents.Count; i++)
            {
                var target = _opponents[i];
                if (target == null)
                {
                    continue;
                }

                var sr = ResolveImpactOnTarget(target, out var made);
                created |= made;
                if (sr == null)
                {
                    continue;
                }

                ApplyPreviewRenderer(sr, impactSprite, true, ResolvePreviewSorting(profile, 2));
                _impacts.Add(sr);
                if (_impact == null)
                {
                    _impact = sr;
                }
            }

            CleanupOrphanImpacts();
            return created;
        }

        private SpriteRenderer ResolveImpactOnTarget(UnitView target, out bool created)
        {
            created = false;
            target.EnsureReceiveDmg();
            var anchor = target.ReceiveDmg;
            if (anchor == null)
            {
                return null;
            }

            var child = anchor.transform.Find(ImpactChildName);
            if (child == null)
            {
                if (!_canMutatePreview && !Application.isPlaying)
                {
                    return null;
                }

                var go = new GameObject(ImpactChildName);
                go.transform.SetParent(anchor.transform, false);
                go.hideFlags = HideFlags.DontSave;
                child = go.transform;
                created = true;
            }

            var sr = child.GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                if (!_canMutatePreview && !Application.isPlaying)
                {
                    return null;
                }

                sr = child.gameObject.AddComponent<SpriteRenderer>();
                created = true;
            }

            sr.gameObject.hideFlags = HideFlags.DontSave;
            return sr;
        }

        private void CleanupOrphanImpacts()
        {
            var keep = new HashSet<SpriteRenderer>(_impacts);
            var views = FindObjectsByType<UnitView>(FindObjectsInactive.Exclude);
            for (var i = 0; i < views.Length; i++)
            {
                var view = views[i];
                var anchor = view != null ? view.ReceiveDmg : null;
                if (anchor == null)
                {
                    continue;
                }

                var child = anchor.transform.Find(ImpactChildName);
                if (child == null)
                {
                    continue;
                }

                var sr = child.GetComponent<SpriteRenderer>();
                if (sr != null && keep.Contains(sr))
                {
                    continue;
                }

                DestroyPreviewObject(child.gameObject);
            }
        }

        private void DestroyAllImpacts()
        {
            for (var i = 0; i < _impacts.Count; i++)
            {
                if (_impacts[i] != null)
                {
                    DestroyPreviewObject(_impacts[i].gameObject);
                }
            }

            _impacts.Clear();
            _impact = null;
            CleanupOrphanImpacts();
        }

        private static void DestroyPreviewObject(GameObject go)
        {
            if (go == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(go);
                return;
            }

            DestroyImmediate(go);
        }

        private void ApplyPreviewRenderer(SpriteRenderer sr, Sprite sprite, bool enabled, int sortingOrder)
        {
            sr.sprite = sprite;
            sr.enabled = enabled && sprite != null;
            sr.color = Color.white;
            sr.sortingLayerID = 0;
            sr.sortingOrder = sortingOrder;
            sr.gameObject.hideFlags = HideFlags.DontSave;
        }

        private int ResolvePreviewSorting(SkillVfxProfileSO profile, int extra)
        {
            var order = profile != null ? profile.sortingOrder : 42;
            var unit = caster != null ? caster.GetComponent<SpriteRenderer>() : null;
            if (unit != null)
            {
                order = Mathf.Max(order, unit.sortingOrder + 20);
            }

            return order + extra;
        }

        private static void NeutralizePreviewRootScale(Transform root)
        {
            if (root == null)
            {
                return;
            }

            var parent = root.parent;
            if (parent == null)
            {
                root.localScale = Vector3.one;
                return;
            }

            var s = parent.lossyScale;
            root.localScale = new Vector3(
                Mathf.Abs(s.x) > 0.0001f ? 1f / s.x : 1f,
                Mathf.Abs(s.y) > 0.0001f ? 1f / s.y : 1f,
                1f);
        }

        private void CleanupLegacyPreviewChildren(Transform root)
        {
            if (!_canMutatePreview && !Application.isPlaying)
            {
                return;
            }

            for (var i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i);
                if (child.name == "Projectile")
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        private bool IsHostedOnUnit => GetComponent<UnitView>() != null;

        private void ApplyLayout(SkillVfxProfileSO profile)
        {
            if (!IsHostedOnUnit)
            {
                transform.position = Vector3.zero;
                transform.localScale = Vector3.one;
            }

            if (profile == null)
            {
                FromWorld = ResolveFrom(null);
                ToWorld = FromWorld + Vector3.left * 4f;
                return;
            }

            var from = ResolveFrom(profile);
            FromWorld = from;
            var primary = PrimaryTarget;
            var firstTo = primary != null
                ? ResolveTo(primary, profile)
                : from + Vector3.left;
            ToWorld = firstTo;
            var previewRoot = transform.Find(PreviewRootName);
            if (previewRoot != null)
            {
                NeutralizePreviewRootScale(previewRoot);
            }
            var worldSize = _previewWorldSize > 0.05f ? _previewWorldSize : profile.projectileWorldSize;
            var travelT = ResolveTravelT(profile);
            var arrived = previewLive && profile.HasArrivedAtB(travelT);
            var hideShotAtB = profile.Travels && arrived && profile.hideProjectileOnArrive;
            var hideHitBeforeContact = profile.PlaysAtTarget && previewLive && !arrived;
            var shot = PrimaryShot;
            if (shot != null)
            {
                var rest = profile.PlaysAtTarget ? firstTo : from;
                var pos = previewLive && profile.Travels
                    ? Vector3.Lerp(from, firstTo, Ease(travelT))
                    : rest;
                var delta = firstTo - from;
                delta.z = 0f;
                if (delta.sqrMagnitude < 0.0001f)
                {
                    delta = Vector3.left;
                }

                shot.transform.SetPositionAndRotation(pos, ResolveProjectileRotation(profile, delta));
                SkillVfxShotView.FitSpriteToWorldSize(shot, worldSize);
                shot.enabled = ShowSprite
                    && shot.sprite != null
                    && !hideShotAtB
                    && !hideHitBeforeContact;
            }

            var flash = !previewLive || arrived || !profile.Travels ? 1f : 0.35f;
            for (var i = 0; i < _opponents.Count; i++)
            {
                var target = _opponents[i];
                var impact = FindImpactOnTarget(target);
                if (impact == null || impact.sprite == null)
                {
                    continue;
                }

                var at = target != null ? ResolveTo(target, profile) : firstTo;
                impact.transform.SetPositionAndRotation(at, Quaternion.identity);
                SkillVfxShotView.FitSpriteToWorldSize(impact, profile.impactWorldSize);
                var c = impact.color;
                impact.color = new Color(c.r, c.g, c.b, flash);
            }
        }

        private static Vector3 ResolveLoadedEuler(SkillVfxProfileSO profile)
        {
            if (profile == null)
            {
                return Vector3.zero;
            }

            if (profile.projectileEuler.sqrMagnitude > 0.0001f)
            {
                return profile.projectileEuler;
            }

            return new Vector3(0f, 0f, profile.facingOffsetDegrees);
        }

        private Quaternion ResolveProjectileRotation(SkillVfxProfileSO profile, Vector3 travelDirection)
        {
            if (profile == null)
            {
                return Quaternion.Euler(_projectileEuler);
            }

            var facing = _projectileEuler.sqrMagnitude > 0.0001f
                ? _projectileEuler.z
                : profile.ResolveProjectileFacingDegrees();
            if (profile.kind is SkillVfxKind.Buff or SkillVfxKind.Hit)
            {
                return Quaternion.Euler(_projectileEuler.x, _projectileEuler.y, facing);
            }

            var travel = ResolveTravelAngle(travelDirection);
            return Quaternion.Euler(_projectileEuler.x, _projectileEuler.y, travel + facing);
        }

        private static float ResolveTravelAngle(Vector3 travelDirection)
        {
            var dir = travelDirection;
            dir.z = 0f;
            if (dir.sqrMagnitude < 0.0001f)
            {
                dir = Vector3.left;
            }

            dir.Normalize();
            return Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        }

        private Vector3 ResolveFrom(SkillVfxProfileSO _)
        {
            return ResolveAnchorFrom() + _fromOffset;
        }

        private Vector3 ResolveTo(UnitView target, SkillVfxProfileSO _)
        {
            return SkillVfxAnchorResolver.ResolveAnchorDestination(target) + _toOffset;
        }

        private Vector3 ResolveAnchorFrom()
        {
            if (projectileA != null)
            {
                return projectileA.transform.position;
            }

            if (caster != null)
            {
                return SkillVfxAnchorResolver.ResolveAnchorOrigin(caster);
            }

            return transform.position;
        }

        public void SetKindHasPattern(SkillVfxKind kind, bool enabled)
        {
            var profile = Profile;
            if (profile == null)
            {
                return;
            }

            if (enabled)
            {
                profile.EnsurePattern(kind);
            }
            else if (profile.kind == kind)
            {
                profile.ClearPattern();
            }

            _animTime = 0f;
            _boundProfile = null;
            RefreshVisual(true);
        }

        private float ResolveTravelT(SkillVfxProfileSO profile)
        {
            if (!profile.Travels)
            {
                if (profile.kind != SkillVfxKind.Hit || !previewLive)
                {
                    return 1f;
                }

                var hitCycle = Mathf.Max(0.4f, profile.travelSeconds + profile.impactSeconds);
                var hitT = loopSlide
                    ? Mathf.Repeat(_animTime, hitCycle)
                    : Mathf.Min(_animTime, hitCycle);
                return Mathf.Clamp01(hitT / hitCycle);
            }

            var cycle = Mathf.Max(0.05f, profile.spawnHoldSeconds + profile.travelSeconds);
            var t = loopSlide
                ? Mathf.Repeat(_animTime, cycle)
                : Mathf.Min(_animTime, cycle);
            if (profile.spawnHoldSeconds <= 0f)
            {
                return Mathf.Clamp01(t / Mathf.Max(0.01f, profile.travelSeconds));
            }

            return t <= profile.spawnHoldSeconds
                ? 0f
                : Mathf.Clamp01((t - profile.spawnHoldSeconds) / Mathf.Max(0.01f, profile.travelSeconds));
        }

        private static float Ease(float travelT)
        {
            return 1f - (1f - travelT) * (1f - travelT);
        }

        private static Sprite ResolveImpactSprite(SkillVfxProfileSO profile)
        {
            if (profile == null || profile.hideImpactSprite)
            {
                return null;
            }

            return profile.impactSprite;
        }

        private Transform EnsurePreviewRoot()
        {
            var root = transform.Find(PreviewRootName);
            if (root == null)
            {
                if (!_canMutatePreview && !Application.isPlaying)
                {
                    return null;
                }

                var go = new GameObject(PreviewRootName);
                go.transform.SetParent(transform, false);
                go.hideFlags = HideFlags.DontSave;
                root = go.transform;
            }

            root.gameObject.hideFlags = HideFlags.DontSave;
            return root;
        }

        private void DestroyPreviewRoot()
        {
            DestroyAllImpacts();
            _shots.Clear();
            _impact = null;
            var root = transform.Find(PreviewRootName);
            if (root == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(root.gameObject);
            }
            else
            {
                DestroyImmediate(root.gameObject);
            }
        }

        private SpriteRenderer ResolvePreviewChild(string childName, ref bool forceRebuild, out bool created)
        {
            created = false;
            var root = EnsurePreviewRoot();
            if (root == null)
            {
                return null;
            }

            var child = root.Find(childName);
            if (child == null)
            {
                if (!_canMutatePreview && !Application.isPlaying)
                {
                    return null;
                }

                var go = new GameObject(childName);
                go.transform.SetParent(root, false);
                child = go.transform;
                forceRebuild = true;
                created = true;
            }

            var sr = child.GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                if (!_canMutatePreview && !Application.isPlaying)
                {
                    return null;
                }

                sr = child.gameObject.AddComponent<SpriteRenderer>();
                forceRebuild = true;
                created = true;
            }

            return sr;
        }

        private static float MeasureWorldSize(SpriteRenderer sr)
        {
            if (sr == null || sr.sprite == null)
            {
                return 0f;
            }

            var native = Mathf.Max(sr.sprite.bounds.size.x, sr.sprite.bounds.size.y);
            var scale = Mathf.Max(
                Mathf.Abs(sr.transform.lossyScale.x),
                Mathf.Abs(sr.transform.lossyScale.y));
            return native * scale;
        }

        private void HidePreviewRenderers()
        {
            for (var i = 0; i < _shots.Count; i++)
            {
                if (_shots[i] != null)
                {
                    _shots[i].enabled = false;
                }
            }

            for (var i = 0; i < _impacts.Count; i++)
            {
                if (_impacts[i] != null)
                {
                    _impacts[i].enabled = false;
                }
            }

            var root = transform.Find(PreviewRootName);
            if (root != null)
            {
                root.gameObject.SetActive((ShowSprite || ShowImpact) && ShouldPresentPreview);
            }
        }

        public UnitView PrimaryTarget
        {
            get
            {
                for (var i = 0; i < _opponents.Count; i++)
                {
                    if (_opponents[i] != null && _opponents[i] != caster)
                    {
                        return _opponents[i];
                    }
                }

                return null;
            }
        }

        public void EnsureOppositeTargets()
        {
            if (HasAssignedTargets() || caster == null)
            {
                return;
            }

            FillOppositeTargets();
        }

        private void RefreshOpponents()
        {
            _opponents.Clear();
            if (!HasAssignedTargets())
            {
                return;
            }

            for (var i = 0; i < targets.Count; i++)
            {
                var other = targets[i];
                if (other == null || other == caster)
                {
                    continue;
                }

                other.EnsureReceiveDmg();
                _opponents.Add(other);
            }
        }

        private static SpriteRenderer FindImpactOnTarget(UnitView target)
        {
            var anchor = target != null ? target.ReceiveDmg : null;
            if (anchor == null)
            {
                return null;
            }

            var child = anchor.transform.Find(ImpactChildName);
            return child != null ? child.GetComponent<SpriteRenderer>() : null;
        }

        private void HidePreviewRoot()
        {
            var root = transform.Find(PreviewRootName);
            if (root != null)
            {
                root.gameObject.SetActive(false);
            }

            for (var i = 0; i < _impacts.Count; i++)
            {
                if (_impacts[i] != null)
                {
                    _impacts[i].enabled = false;
                }
            }
        }

        private bool HasAssignedTargets()
        {
            if (targets == null)
            {
                return false;
            }

            for (var i = 0; i < targets.Count; i++)
            {
                if (targets[i] != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void ApplyCasterAttackPose()
        {
            if (caster == null)
            {
                return;
            }

            var sim = caster.GetComponent<UnitSpriteSimulator>();
            if (sim == null)
            {
                return;
            }

            var state = UnitView.ResolveAttackVisualState(skill);
            var mode = caster.Side == GridSide.Player
                ? UnitSpriteApplyMode.Auto
                : ResolveEnemyPoseMode(sim, state);
            if (sim.ApplyLayoutForState(state, keepWorldFeet: true, mode))
            {
                _poseApplied = true;
                TickCasterPose();
            }
        }

        private static UnitSpriteApplyMode ResolveEnemyPoseMode(
            UnitSpriteSimulator sim,
            UnitCombatVisualState state)
        {
            if (sim.TryGetLayout(state, out var layout) && layout.HasStillSprite)
            {
                return UnitSpriteApplyMode.PreferStill;
            }

            return UnitSpriteApplyMode.Auto;
        }

        private void TickCasterPose()
        {
            if (caster == null || caster.Side != GridSide.Player)
            {
                return;
            }

            var sim = caster.GetComponent<UnitSpriteSimulator>();
            if (sim == null || !sim.TryGetLayout(UnitView.ResolveAttackVisualState(skill), out var layout))
            {
                return;
            }

            var clip = layout.animationClip;
            if (clip == null)
            {
                return;
            }

            var length = Mathf.Max(0.01f, clip.length);
            clip.SampleAnimation(caster.gameObject, Mathf.Repeat(_animTime, length));
        }

        private void RestoreCasterIdle()
        {
            if (!_poseApplied || caster == null)
            {
                return;
            }

            var sim = caster.GetComponent<UnitSpriteSimulator>();
            sim?.ApplyLayoutForState(UnitCombatVisualState.Idle, keepWorldFeet: true);
            _poseApplied = false;
        }

        private string ResolvePoseClipName(UnitCombatVisualState state)
        {
            if (caster == null)
            {
                return "—";
            }

            var sim = caster.GetComponent<UnitSpriteSimulator>();
            if (sim != null && sim.TryGetLayout(state, out var layout))
            {
                if (layout.animationClip != null)
                {
                    return layout.animationClip.name;
                }

                if (layout.sprite != null)
                {
                    return layout.sprite.name;
                }
            }

            return "—";
        }

        private static SkillDefinitionSO ResolveSkillFromUnit(UnitView view)
        {
            var preset = view != null ? view.ResolvePreset() : null;
            var skills = preset != null ? preset.skills : null;
            if (skills != null)
            {
                for (var i = 0; i < skills.Length; i++)
                {
                    if (skills[i] != null && skills[i].vfxProfile != null)
                    {
                        return skills[i];
                    }
                }

                for (var i = 0; i < skills.Length; i++)
                {
                    if (skills[i] != null)
                    {
                        return skills[i];
                    }
                }
            }

            return LoadSkillForDemoKey(view != null ? view.DemoUnitKey : null);
        }

        private static SkillDefinitionSO LoadSkillForDemoKey(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            var id = key switch
            {
                "boss_despair" => "boss_despair_core",
                "ren" => "ren_basic",
                "tank" or "charlotte" or "charlott" => "Charlott_basic",
                "mage" or "coda" => "Coda_basic",
                "grunt" or "grunt_left" or "grunt_right" => "grunt_strike",
                "kiki" or "kiki_ueda" => "kiki_claw",
                _ => null
            };

            if (id != null)
            {
                var named = Resources.Load<SkillDefinitionSO>($"Skills/{id}");
                if (named != null)
                {
                    return named;
                }
            }

            return Resources.Load<SkillDefinitionSO>($"Skills/{key}_basic")
                   ?? Resources.Load<SkillDefinitionSO>($"Skills/{key}_strike")
                   ?? Resources.Load<SkillDefinitionSO>($"Skills/{key}");
        }
    }
}
