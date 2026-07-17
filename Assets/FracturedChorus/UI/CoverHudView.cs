using FracturedChorus.Combat.Core;
using FracturedChorus.Combat.Cover;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.UI
{
    public class CoverHudView : MonoBehaviour
    {
        private const string ButtonSpritePath = "UI/Combat/combat_btn_cover_v1";

        private CombatSession _session;
        private Image _fill;
        private Text _gaugeLabel;
        private Text _statusLabel;
        private Button _button;
        private Text _buttonLabel;

        public static CoverHudView EnsureOn(RectTransform parent)
        {
            if (parent == null)
            {
                return null;
            }

            var existing = parent.Find("CoverHud")?.GetComponent<CoverHudView>();
            if (existing == null)
            {
                existing = Object.FindAnyObjectByType<CoverHudView>();
            }

            if (existing != null)
            {
                existing.ApplyScreenCornerLayout(parent);
                existing.EnsureBuilt();
                return existing;
            }

            var go = new GameObject("CoverHud", typeof(RectTransform));
            var view = go.AddComponent<CoverHudView>();
            view.ApplyScreenCornerLayout(parent);
            view.EnsureBuilt();
            return view;
        }

        private void ApplyScreenCornerLayout(RectTransform canvasParent)
        {
            var rt = transform as RectTransform;
            if (rt == null || canvasParent == null)
            {
                return;
            }

            if (rt.parent != canvasParent)
            {
                rt.SetParent(canvasParent, false);
            }

            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-16f, -16f);
            rt.sizeDelta = new Vector2(148f, 52f);
            rt.SetAsLastSibling();
        }

        public void EnsureBuilt()
        {
            var root = transform as RectTransform;
            if (root == null)
            {
                return;
            }

            if (_fill == null)
            {
                var barGo = root.Find("GaugeBar");
                if (barGo == null)
                {
                    barGo = new GameObject("GaugeBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image))
                        .transform;
                    barGo.SetParent(root, false);
                    var barRt = barGo.GetComponent<RectTransform>();
                    barRt.anchorMin = new Vector2(0f, 0.55f);
                    barRt.anchorMax = new Vector2(1f, 1f);
                    barRt.offsetMin = Vector2.zero;
                    barRt.offsetMax = Vector2.zero;
                    var bg = barGo.GetComponent<Image>();
                    bg.color = new Color(0.15f, 0.16f, 0.2f, 0.85f);
                    bg.raycastTarget = false;
                }

                var fillT = barGo.Find("Fill");
                if (fillT == null)
                {
                    var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    fillT = fillGo.transform;
                    fillT.SetParent(barGo, false);
                    var fillRt = fillGo.GetComponent<RectTransform>();
                    fillRt.anchorMin = Vector2.zero;
                    fillRt.anchorMax = Vector2.one;
                    fillRt.offsetMin = new Vector2(2f, 2f);
                    fillRt.offsetMax = new Vector2(-2f, -2f);
                    _fill = fillGo.GetComponent<Image>();
                    _fill.sprite = UiCircleSpriteUtil.Circle;
                    _fill.type = Image.Type.Filled;
                    _fill.fillMethod = Image.FillMethod.Horizontal;
                    _fill.color = new Color(0.95f, 0.75f, 0.35f, 1f);
                    _fill.raycastTarget = false;
                }
                else
                {
                    _fill = fillT.GetComponent<Image>();
                }
            }

            if (_gaugeLabel == null)
            {
                _gaugeLabel = EnsureText(root, "GaugeLabel", new Vector2(0f, 0.55f), new Vector2(1f, 1f), 12);
            }

            if (_statusLabel == null)
            {
                _statusLabel = EnsureText(root, "StatusLabel", new Vector2(0f, 0.35f), new Vector2(1f, 0.55f), 11);
            }

            if (_button == null)
            {
                var btnT = root.Find("CoverButton");
                if (btnT == null)
                {
                    var btnGo = new GameObject("CoverButton", typeof(RectTransform), typeof(CanvasRenderer),
                        typeof(Image), typeof(Button));
                    btnT = btnGo.transform;
                    btnT.SetParent(root, false);
                    var btnRt = btnGo.GetComponent<RectTransform>();
                    btnRt.anchorMin = new Vector2(0f, 0f);
                    btnRt.anchorMax = new Vector2(1f, 0.35f);
                    btnRt.offsetMin = Vector2.zero;
                    btnRt.offsetMax = Vector2.zero;
                    var img = btnGo.GetComponent<Image>();
                    try
                    {
                        var sprite = Resources.Load<Sprite>(ButtonSpritePath);
                        if (sprite != null)
                        {
                            img.sprite = sprite;
                            img.type = Image.Type.Sliced;
                        }
                        else
                        {
                            img.color = new Color(0.35f, 0.4f, 0.55f, 0.95f);
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError("[CoverHud] Failed to load cover button sprite: " + e);
                        img.color = new Color(0.35f, 0.4f, 0.55f, 0.95f);
                    }

                    _button = btnGo.GetComponent<Button>();
                }
                else
                {
                    _button = btnT.GetComponent<Button>();
                }

                if (_button != null)
                {
                    _button.onClick.RemoveListener(OnCoverClicked);
                    _button.onClick.AddListener(OnCoverClicked);
                }

                _buttonLabel = EnsureText(btnT as RectTransform, "Label", Vector2.zero, Vector2.one, 14);
                if (_buttonLabel != null)
                {
                    _buttonLabel.text = "COVER";
                    _buttonLabel.alignment = TextAnchor.MiddleCenter;
                    _buttonLabel.fontStyle = FontStyle.Bold;
                }
            }
        }

        public void Bind(CombatSession session)
        {
            if (_session != null)
            {
                _session.OnPhaseChanged -= OnPhaseChanged;
                if (_session.Cover != null)
                {
                    _session.Cover.OnChanged -= Refresh;
                }
            }

            _session = session;
            EnsureBuilt();

            if (_session != null)
            {
                _session.OnPhaseChanged += OnPhaseChanged;
                _session.Cover.OnChanged += Refresh;
            }

            Refresh();
        }

        private void OnDestroy()
        {
            if (_session == null)
            {
                return;
            }

            _session.OnPhaseChanged -= OnPhaseChanged;
            if (_session.Cover != null)
            {
                _session.Cover.OnChanged -= Refresh;
            }
        }

        private void OnPhaseChanged(CombatPhase _) => Refresh();

        private void OnCoverClicked()
        {
            if (_session?.Cover == null)
            {
                return;
            }

            var renAlive = IsRenAlive(_session);
            if (!_session.AllowCoverActivate)
            {
                Debug.Log("[Cover] Button ignored — not in planning stop (AllowCoverActivate=false).");
                return;
            }

            if (!_session.Cover.TryActivate(renAlive))
            {
                Debug.Log(
                    $"[Cover] Cannot activate — gauge {_session.Cover.Gauge}/{CoverConstants.ActivateCost}" +
                    $" pending={_session.Cover.IsPending} active={_session.Cover.IsActive} renAlive={renAlive}");
                return;
            }

            Refresh();
        }

        public void Refresh()
        {
            EnsureBuilt();
            if (_session?.Cover == null)
            {
                return;
            }

            var cover = _session.Cover;
            var ratio = CoverConstants.GaugeCap <= 0
                ? 0f
                : cover.Gauge / (float)CoverConstants.GaugeCap;
            if (_fill != null)
            {
                _fill.fillAmount = Mathf.Clamp01(ratio);
            }

            if (_gaugeLabel != null)
            {
                _gaugeLabel.text = $"COVER {cover.Gauge}/{CoverConstants.GaugeCap}";
                _gaugeLabel.alignment = TextAnchor.MiddleCenter;
            }

            if (_statusLabel != null)
            {
                if (cover.IsActive)
                {
                    _statusLabel.text = $"ACTIVE {cover.ActiveBeatsRemaining}";
                }
                else if (cover.IsPending)
                {
                    _statusLabel.text = "PENDING";
                }
                else
                {
                    _statusLabel.text = string.Empty;
                }

                _statusLabel.alignment = TextAnchor.MiddleCenter;
            }

            if (_button != null)
            {
                var canPress = _session.AllowCoverActivate &&
                               cover.CanActivate(IsRenAlive(_session));
                _button.interactable = canPress;
                var cg = _button.GetComponent<CanvasGroup>();
                if (cg == null)
                {
                    cg = _button.gameObject.AddComponent<CanvasGroup>();
                }

                cg.blocksRaycasts = true;
                cg.interactable = canPress;
                if (_buttonLabel != null)
                {
                    _buttonLabel.color = canPress ? Color.white : new Color(1f, 1f, 1f, 0.45f);
                }
            }
        }

        private static bool IsRenAlive(CombatSession session)
        {
            if (session?.Grid == null)
            {
                return false;
            }

            foreach (var u in session.Grid.PlayerUnits)
            {
                if (u != null &&
                    u.IsAlive &&
                    string.Equals(u.DisplayName, CoverConstants.RenDisplayName, System.StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static Text EnsureText(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            int fontSize)
        {
            if (parent == null)
            {
                return null;
            }

            var t = parent.Find(name);
            Text text;
            if (t == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
                t = go.transform;
                t.SetParent(parent, false);
                text = go.GetComponent<Text>();
                text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (text.font == null)
                {
                    text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                }

                text.color = Color.white;
                text.raycastTarget = false;
            }
            else
            {
                text = t.GetComponent<Text>();
            }

            var rt = t.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            text.fontSize = fontSize;
            return text;
        }
    }
}
