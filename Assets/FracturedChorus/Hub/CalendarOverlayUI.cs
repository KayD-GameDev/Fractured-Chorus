using System;
using System.Collections.Generic;
using FracturedChorus.Meta;
using UnityEngine;
using UnityEngine.UI;
using FracturedChorus.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FracturedChorus.Hub
{
    public sealed class CalendarOverlayUI : MonoBehaviour
    {
        public readonly struct BuildResult
        {
            public BuildResult(CalendarOverlayUI overlay)
            {
                Overlay = overlay;
            }

            public CalendarOverlayUI Overlay { get; }
        }

        private static readonly string[] WeekdayNames = { "SUN", "MON", "TUE", "WED", "THU", "FRI", "SAT" };

        private const int DisplayYear = 2026;
        private const int ArcMonth = 9;
        private const int MinViewMonth = 9;
        private const int MaxViewMonth = 10;

        [SerializeField] private GameObject root;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text yearLabel;
        [SerializeField] private Text monthBigLabel;
        [SerializeField] private Text monthNextLabel;
        [SerializeField] private Text dateChipLabel;
        [SerializeField] private Image dateChipBackground;
        [SerializeField] private RectTransform todayMarkerRoot;
        [SerializeField] private Text todayMarkerLabel;
        [SerializeField] private Image todayTriangle;
        [SerializeField] private RectTransform weekdayRow;
        [SerializeField] private RectTransform gridRoot;
        [SerializeField] private Text selectedDayInfoLabel;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button hintQButton;
        [SerializeField] private Button hintEButton;
        [SerializeField] private TownMapSfxController sfx;

        private readonly List<DayCell> _cells = new List<DayCell>(42);
        private GameMetaState _state;
        private Action _onClosed;
        private bool _wired;
        private int _viewMonth = ArcMonth;
        private int _selectedDay = -1;

        private sealed class DayCell
        {
            public int DayNumber;
            public Button Button;
            public Text Label;
            public Image Ring;
            public Image Dot;
            public Image TodayGlow;
            public RectTransform Rect;
        }

        public bool IsOpen => root != null && root.activeSelf;

        public void BindSfx(TownMapSfxController controller)
        {
            sfx = controller;
        }

        public void Show(GameMetaState state, Action onClosed = null)
        {
            _state = state;
            _onClosed = onClosed;
            var currentMonth = state?.Calendar.CurrentDate.Month ?? ArcMonth;
            _viewMonth = Mathf.Clamp(currentMonth, MinViewMonth, MaxViewMonth);
            _selectedDay = state?.Calendar.CurrentDate.Day ?? 1;
            EnsureRuntimeBindings();
            Wire();
            if (root != null)
            {
                root.SetActive(true);
            }

            sfx?.PlayOpenPanel();
            Refresh();
        }

        public void Hide()
        {
            if (IsOpen)
            {
                sfx?.PlayClosePanel();
            }

            if (root != null)
            {
                root.SetActive(false);
            }

            var callback = _onClosed;
            _onClosed = null;
            callback?.Invoke();
        }

        private void Update()
        {
            if (!IsOpen)
            {
                return;
            }

            if (TownMapInput.MonthPrevPressed())
            {
                ShiftMonth(-1);
                return;
            }

            if (TownMapInput.MonthNextPressed())
            {
                ShiftMonth(1);
                return;
            }

            if (TownMapInput.CancelPressed())
            {
                Hide();
            }
        }

        public static BuildResult Build(Transform parent)
        {
            var existing = parent.Find("CalendarOverlay");
            if (existing != null)
            {
                var hasWeekdays = existing.Find("LeftPanel/WeekdayRow") != null
                                  || existing.Find("L01_LeftPanel/L03_WeekdayRow") != null;
                if (!hasWeekdays)
                {
                    existing.gameObject.name = "CalendarOverlay_OLD";
                    if (Application.isPlaying)
                    {
                        UnityEngine.Object.Destroy(existing.gameObject);
                    }
                    else
                    {
                        UnityEngine.Object.DestroyImmediate(existing.gameObject);
                    }

                    existing = null;
                }
            }

            if (existing != null)
            {
                var overlay = existing.GetComponent<CalendarOverlayUI>()
                              ?? existing.gameObject.AddComponent<CalendarOverlayUI>();
                overlay.EnsureRuntimeBindings();
                overlay.Rewire();
                return new BuildResult(overlay);
            }

            return new BuildResult(CreateHierarchy(parent));
        }

        public void EnsureRuntimeBindings()
        {
            if (root == null)
            {
                root = gameObject;
            }

            if (gridRoot == null)
            {
                gridRoot = FindRect(transform, "LeftPanel/DayGrid", "L01_LeftPanel/L04_DayGrid", "DayGrid", "L04_DayGrid");
            }

            if (weekdayRow == null)
            {
                weekdayRow = FindRect(transform, "LeftPanel/WeekdayRow", "L01_LeftPanel/L03_WeekdayRow", "WeekdayRow", "L03_WeekdayRow");
            }

            if (todayMarkerRoot == null)
            {
                todayMarkerRoot = FindRect(transform, "TodayMarker", "L06_TodayMarker");
            }

            if (dateChipLabel == null)
            {
                var chip = FindTransform(transform, "DateChipBg/DateChip", "L05_DateChip/DateChip");
                if (chip != null)
                {
                    dateChipLabel = chip.GetComponent<Text>();
                }
            }

            if (selectedDayInfoLabel == null)
            {
                var info = FindTransform(transform, "LeftPanel/SelectedDayInfo", "L01_LeftPanel/L04b_SelectedDayInfo");
                if (info != null)
                {
                    selectedDayInfoLabel = info.GetComponent<Text>();
                }
            }

            if (hintQButton == null)
            {
                hintQButton = FindButton(transform, "LeftPanel/HintQ", "L01_LeftPanel/HintQ", "HintQ");
            }

            if (hintEButton == null)
            {
                hintEButton = FindButton(transform, "LeftPanel/HintE", "L01_LeftPanel/HintE", "HintE");
            }

            if (monthBigLabel == null)
            {
                var monthBig = FindTransform(transform, "LeftPanel/MonthBig", "L01_LeftPanel/L02a_MonthBig", "MonthBig");
                if (monthBig != null)
                {
                    monthBigLabel = monthBig.GetComponent<Text>();
                }
            }

            if (monthNextLabel == null)
            {
                var monthNext = FindTransform(transform, "LeftPanel/MonthNext", "L01_LeftPanel/L02b_MonthNext", "MonthNext");
                if (monthNext != null)
                {
                    monthNextLabel = monthNext.GetComponent<Text>();
                }
            }

            if (_cells.Count == 0 && gridRoot != null)
            {
                if (gridRoot.childCount == 42)
                {
                    RebindDayCellsFromHierarchy();
                }
                else
                {
                    BuildDayCells();
                }
            }
        }

        private static RectTransform FindRect(Transform root, params string[] paths)
        {
            var tf = FindTransform(root, paths);
            return tf as RectTransform;
        }

        private static Button FindButton(Transform root, params string[] paths)
        {
            var tf = FindTransform(root, paths);
            if (tf == null)
            {
                return null;
            }

            var button = tf.GetComponent<Button>();
            if (button != null)
            {
                return button;
            }

            var image = tf.GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = true;
            }

            button = tf.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            return button;
        }

        private static Transform FindTransform(Transform root, params string[] paths)
        {
            foreach (var path in paths)
            {
                var found = root.Find(path);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private void RebindDayCellsFromHierarchy()
        {
            _cells.Clear();
            for (var i = 0; i < gridRoot.childCount; i++)
            {
                var cellGo = gridRoot.GetChild(i);
                var button = cellGo.GetComponent<Button>();
                var label = cellGo.Find("Num")?.GetComponent<Text>();
                var ring = cellGo.Find("Ring")?.GetComponent<Image>();
                var glow = cellGo.Find("TodayGlow")?.GetComponent<Image>();
                var dot = cellGo.Find("Dot")?.GetComponent<Image>();
                if (button == null || label == null)
                {
                    continue;
                }

                button.onClick.RemoveAllListeners();
                var capturedIndex = i;
                button.onClick.AddListener(() => OnDayClicked(capturedIndex));

                _cells.Add(new DayCell
                {
                    Button = button,
                    Label = label,
                    Ring = ring,
                    Dot = dot,
                    TodayGlow = glow,
                    Rect = cellGo as RectTransform
                });
            }
        }

        private static CalendarOverlayUI CreateHierarchy(Transform parent)
        {
            var bgSprite = LoadSprite("calendar_ren_panel_v1");

            var rootGo = new GameObject("CalendarOverlay", typeof(RectTransform));
            rootGo.transform.SetParent(parent, false);
            Stretch(rootGo.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            var bg = CreateImage(rootGo.transform, "Background", bgSprite);
            Stretch(bg.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            bg.preserveAspect = false;
            bg.raycastTarget = true;

            var left = new GameObject("LeftPanel", typeof(RectTransform), typeof(Image));
            left.transform.SetParent(rootGo.transform, false);
            Stretch(left.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0.56f, 1f), Vector2.zero, Vector2.zero);
            var leftImg = left.GetComponent<Image>();
            leftImg.color = FcColorTokens.WithAlpha(FcColorTokens.Surface.Panel, 0.62f);
            leftImg.raycastTarget = false;

            var title = CreateText(left.transform, "Title", "CALENDAR", 52, TextAnchor.MiddleLeft);
            Stretch(title.rectTransform, new Vector2(0.06f, 0.9f), new Vector2(0.75f, 0.98f), Vector2.zero, Vector2.zero);
            title.fontStyle = FontStyle.Bold;
            title.color = Color.white;

            var year = CreateText(left.transform, "Year", DisplayYear.ToString(), 24, TextAnchor.MiddleLeft);
            Stretch(year.rectTransform, new Vector2(0.06f, 0.84f), new Vector2(0.35f, 0.9f), Vector2.zero, Vector2.zero);
            year.color = FcColorTokens.Brand.Cyan;

            var monthBig = CreateText(left.transform, "MonthBig", ArcMonth.ToString(), 140, TextAnchor.LowerCenter);
            Stretch(monthBig.rectTransform, new Vector2(0.12f, 0.66f), new Vector2(0.4f, 0.86f), Vector2.zero, Vector2.zero);
            monthBig.fontStyle = FontStyle.Bold;
            monthBig.color = new Color(0.02f, 0.06f, 0.22f, 0.98f);

            var monthNext = CreateText(left.transform, "MonthNext", "10", 48, TextAnchor.MiddleLeft);
            Stretch(monthNext.rectTransform, new Vector2(0.44f, 0.72f), new Vector2(0.62f, 0.84f), Vector2.zero, Vector2.zero);
            monthNext.color = new Color(1f, 1f, 1f, 0.22f);

            var arrow = CreateText(left.transform, "MonthArrow", "▶", 22, TextAnchor.MiddleCenter);
            Stretch(arrow.rectTransform, new Vector2(0.4f, 0.74f), new Vector2(0.46f, 0.82f), Vector2.zero, Vector2.zero);
            arrow.color = FcColorTokens.Semantic.CalendarPink;

            var hintQ = CreateKeyHint(left.transform, "HintQ", "Q", new Vector2(0.05f, 0.74f), new Vector2(0.12f, 0.82f));
            var hintE = CreateKeyHint(left.transform, "HintE", "E", new Vector2(0.62f, 0.74f), new Vector2(0.69f, 0.82f));

            var weekdayRowGo = new GameObject("WeekdayRow", typeof(RectTransform));
            weekdayRowGo.transform.SetParent(left.transform, false);
            Stretch(weekdayRowGo.GetComponent<RectTransform>(), new Vector2(0.06f, 0.6f), new Vector2(0.94f, 0.66f), Vector2.zero, Vector2.zero);
            for (var i = 0; i < 7; i++)
            {
                var wd = CreateText(weekdayRowGo.transform, WeekdayNames[i], WeekdayNames[i], 18, TextAnchor.MiddleCenter);
                var x0 = i / 7f;
                var x1 = (i + 1) / 7f;
                Stretch(wd.rectTransform, new Vector2(x0, 0f), new Vector2(x1, 1f), Vector2.zero, Vector2.zero);
                wd.fontStyle = FontStyle.Bold;
                wd.color = i == 0 ? FcColorTokens.Semantic.CalendarSunday : Color.white;
            }

            var grid = new GameObject("DayGrid", typeof(RectTransform));
            grid.transform.SetParent(left.transform, false);
            Stretch(grid.GetComponent<RectTransform>(), new Vector2(0.05f, 0.14f), new Vector2(0.95f, 0.6f), Vector2.zero, Vector2.zero);

            var info = CreateText(left.transform, "SelectedDayInfo", string.Empty, 18, TextAnchor.MiddleLeft);
            Stretch(info.rectTransform, new Vector2(0.06f, 0.04f), new Vector2(0.7f, 0.12f), Vector2.zero, Vector2.zero);
            info.color = FcColorTokens.Brand.Cyan;

            var chipBgGo = new GameObject("DateChipBg", typeof(RectTransform), typeof(Image));
            chipBgGo.transform.SetParent(rootGo.transform, false);
            Stretch(chipBgGo.GetComponent<RectTransform>(), new Vector2(0.5f, 0.46f), new Vector2(0.62f, 0.53f), Vector2.zero, Vector2.zero);
            var chipBg = chipBgGo.GetComponent<Image>();
            chipBg.color = FcColorTokens.Surface.Chip;
            chipBg.raycastTarget = false;

            var chip = CreateText(chipBgGo.transform, "DateChip", "9/1", 22, TextAnchor.MiddleCenter);
            Stretch(chip.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            chip.fontStyle = FontStyle.Bold;
            chip.color = new Color(0.02f, 0.06f, 0.2f);

            var todayRoot = new GameObject("TodayMarker", typeof(RectTransform));
            todayRoot.transform.SetParent(rootGo.transform, false);
            var todayRootRect = todayRoot.GetComponent<RectTransform>();
            todayRootRect.sizeDelta = new Vector2(72f, 48f);

            var tri = new GameObject("Triangle", typeof(RectTransform), typeof(Image));
            tri.transform.SetParent(todayRoot.transform, false);
            Stretch(tri.GetComponent<RectTransform>(), new Vector2(0.25f, 0f), new Vector2(0.75f, 0.45f), Vector2.zero, Vector2.zero);
            var triImg = tri.GetComponent<Image>();
            triImg.color = FcColorTokens.Semantic.CalendarPink;
            triImg.raycastTarget = false;
            tri.transform.localEulerAngles = new Vector3(0f, 0f, 180f);

            var todayLabel = CreateText(todayRoot.transform, "Label", "TODAY", 14, TextAnchor.MiddleCenter);
            Stretch(todayLabel.rectTransform, new Vector2(0f, 0.4f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
            todayLabel.fontStyle = FontStyle.Bold;
            todayLabel.color = Color.white;
            todayRoot.SetActive(false);

            var close = CreateCloseButton(rootGo.transform);

            var overlay = rootGo.AddComponent<CalendarOverlayUI>();
            overlay.root = rootGo;
            overlay.backgroundImage = bg;
            overlay.titleLabel = title;
            overlay.yearLabel = year;
            overlay.monthBigLabel = monthBig;
            overlay.monthNextLabel = monthNext;
            overlay.dateChipLabel = chip;
            overlay.dateChipBackground = chipBg;
            overlay.todayMarkerRoot = todayRootRect;
            overlay.todayMarkerLabel = todayLabel;
            overlay.todayTriangle = triImg;
            overlay.weekdayRow = weekdayRowGo.GetComponent<RectTransform>();
            overlay.gridRoot = grid.GetComponent<RectTransform>();
            overlay.selectedDayInfoLabel = info;
            overlay.closeButton = close;
            overlay.hintQButton = hintQ;
            overlay.hintEButton = hintE;
            overlay.BuildDayCells();
            overlay.Rewire();
            rootGo.SetActive(false);
            return overlay;
        }

        public void Rewire()
        {
            _wired = false;
            Wire();
        }

        private void Wire()
        {
            if (_wired)
            {
                return;
            }

            if (root == null)
            {
                root = gameObject;
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Hide);
            }

            if (hintQButton != null)
            {
                hintQButton.onClick.RemoveAllListeners();
                hintQButton.onClick.AddListener(() => ShiftMonth(-1));
            }

            if (hintEButton != null)
            {
                hintEButton.onClick.RemoveAllListeners();
                hintEButton.onClick.AddListener(() => ShiftMonth(1));
            }

            if (_cells.Count == 0 && gridRoot != null)
            {
                BuildDayCells();
            }

            _wired = true;
        }

        private void ShiftMonth(int delta)
        {
            var target = Mathf.Clamp(_viewMonth + delta, MinViewMonth, MaxViewMonth);
            if (target == _viewMonth)
            {
                return;
            }

            _viewMonth = target;
            var daysInMonth = GameDate.GetDaysInMonth(_viewMonth);
            if (_selectedDay > daysInMonth)
            {
                _selectedDay = daysInMonth;
            }

            sfx?.PlaySelect();
            Refresh();
        }

        private void BuildDayCells()
        {
            _cells.Clear();
            while (gridRoot.childCount > 0)
            {
                DestroyImmediateSafe(gridRoot.GetChild(0).gameObject);
            }

            for (var row = 0; row < 6; row++)
            {
                for (var col = 0; col < 7; col++)
                {
                    var index = (row * 7) + col;
                    var cellGo = new GameObject($"Day_{index}", typeof(RectTransform), typeof(Image), typeof(Button));
                    cellGo.transform.SetParent(gridRoot, false);
                    var rect = cellGo.GetComponent<RectTransform>();
                    var x0 = col / 7f;
                    var x1 = (col + 1) / 7f;
                    var y1 = 1f - (row / 6f);
                    var y0 = 1f - ((row + 1) / 6f);
                    Stretch(rect, new Vector2(x0, y0), new Vector2(x1, y1), Vector2.zero, Vector2.zero);

                    var hit = cellGo.GetComponent<Image>();
                    hit.color = new Color(1f, 1f, 1f, 0.001f);
                    hit.raycastTarget = true;

                    var ringGo = new GameObject("Ring", typeof(RectTransform), typeof(Image));
                    ringGo.transform.SetParent(cellGo.transform, false);
                    Stretch(ringGo.GetComponent<RectTransform>(), new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.88f), Vector2.zero, Vector2.zero);
                    var ring = ringGo.GetComponent<Image>();
                    ring.color = Color.clear;
                    ring.raycastTarget = false;

                    var glowGo = new GameObject("TodayGlow", typeof(RectTransform), typeof(Image));
                    glowGo.transform.SetParent(cellGo.transform, false);
                    Stretch(glowGo.GetComponent<RectTransform>(), new Vector2(0.18f, 0.18f), new Vector2(0.82f, 0.82f), Vector2.zero, Vector2.zero);
                    var glow = glowGo.GetComponent<Image>();
                    glow.color = Color.clear;
                    glow.raycastTarget = false;

                    var dotGo = new GameObject("Dot", typeof(RectTransform), typeof(Image));
                    dotGo.transform.SetParent(cellGo.transform, false);
                    Stretch(dotGo.GetComponent<RectTransform>(), new Vector2(0.42f, 0.06f), new Vector2(0.58f, 0.18f), Vector2.zero, Vector2.zero);
                    var dot = dotGo.GetComponent<Image>();
                    dot.color = Color.clear;
                    dot.raycastTarget = false;

                    var label = CreateText(cellGo.transform, "Num", string.Empty, 30, TextAnchor.MiddleCenter);
                    Stretch(label.rectTransform, new Vector2(0.05f, 0.18f), new Vector2(0.95f, 0.95f), Vector2.zero, Vector2.zero);
                    label.fontStyle = FontStyle.Bold;

                    var button = cellGo.GetComponent<Button>();
                    button.targetGraphic = hit;
                    var capturedIndex = index;
                    button.onClick.AddListener(() => OnDayClicked(capturedIndex));

                    _cells.Add(new DayCell
                    {
                        Button = button,
                        Label = label,
                        Ring = ring,
                        Dot = dot,
                        TodayGlow = glow,
                        Rect = rect
                    });
                }
            }
        }

        private void OnDayClicked(int cellIndex)
        {
            if (cellIndex < 0 || cellIndex >= _cells.Count)
            {
                return;
            }

            var day = _cells[cellIndex].DayNumber;
            if (day <= 0)
            {
                return;
            }

            _selectedDay = day;
            sfx?.PlaySelect();
            Refresh();
        }

        private void Refresh()
        {
            if (_state == null)
            {
                return;
            }

            var current = _state.Calendar.CurrentDate;
            var month = _viewMonth;
            var daysInMonth = GameDate.GetDaysInMonth(month);
            var startWeekday = (int)new DateTime(DisplayYear, month, 1).DayOfWeek;
            var eventDays = month == ArcMonth ? CollectEventDays(_state) : new HashSet<int>();
            DayCell todayCell = null;

            if (monthBigLabel != null)
            {
                monthBigLabel.text = month.ToString();
            }

            if (yearLabel != null)
            {
                yearLabel.text = DisplayYear.ToString();
            }

            if (monthNextLabel != null)
            {
                if (month >= MaxViewMonth)
                {
                    monthNextLabel.text = string.Empty;
                    monthNextLabel.color = new Color(1f, 1f, 1f, 0.08f);
                }
                else
                {
                    monthNextLabel.text = (month + 1).ToString();
                    monthNextLabel.color = new Color(1f, 1f, 1f, 0.22f);
                }
            }

            RefreshMonthHints();

            for (var i = 0; i < _cells.Count; i++)
            {
                var cell = _cells[i];
                var dayNumber = (i - startWeekday) + 1;
                cell.DayNumber = dayNumber;

                if (dayNumber < 1 || dayNumber > daysInMonth)
                {
                    cell.Label.text = string.Empty;
                    cell.Ring.color = Color.clear;
                    cell.Dot.color = Color.clear;
                    cell.TodayGlow.color = Color.clear;
                    cell.Button.interactable = false;
                    continue;
                }

                cell.Button.interactable = true;
                cell.Label.text = dayNumber.ToString();
                var weekday = (startWeekday + dayNumber - 1) % 7;
                cell.Label.color = weekday switch
                {
                    0 => FcColorTokens.Semantic.CalendarSunday,
                    6 => FcColorTokens.Brand.SaturdayLabel,
                    _ => Color.white
                };
                cell.Label.fontSize = 30;

                var isToday = current.Month == month && current.Day == dayNumber;
                var isSelected = _selectedDay == dayNumber;

                if (isToday)
                {
                    cell.Label.color = FcColorTokens.Semantic.CalendarPink;
                    cell.Label.fontSize = 36;
                    cell.TodayGlow.color = new Color(FcColorTokens.Semantic.CalendarPink.r, FcColorTokens.Semantic.CalendarPink.g, FcColorTokens.Semantic.CalendarPink.b, 0.2f);
                    todayCell = cell;
                }
                else
                {
                    cell.TodayGlow.color = Color.clear;
                }

                if (isSelected && !isToday)
                {
                    cell.Label.fontSize = 34;
                    cell.TodayGlow.color = new Color(FcColorTokens.Brand.Cyan.r, FcColorTokens.Brand.Cyan.g, FcColorTokens.Brand.Cyan.b, 0.18f);
                }

                if (eventDays.Contains(dayNumber))
                {
                    cell.Ring.color = FcColorTokens.WithAlpha(FcColorTokens.Semantic.EventGold, 0.9f);
                    cell.Dot.color = Color.white;
                }
                else
                {
                    cell.Ring.color = Color.clear;
                    cell.Dot.color = Color.clear;
                }
            }

            var chipDay = _selectedDay > 0 ? _selectedDay : current.Day;
            if (chipDay < 1 || chipDay > daysInMonth)
            {
                chipDay = Mathf.Clamp(current.Month == month ? current.Day : 1, 1, daysInMonth);
                _selectedDay = chipDay;
            }

            if (dateChipLabel != null)
            {
                dateChipLabel.text = $"{month}/{chipDay}";
            }

            if (selectedDayInfoLabel != null)
            {
                selectedDayInfoLabel.text = BuildDayInfo(month, chipDay, current, eventDays);
            }

            if (todayMarkerRoot != null)
            {
                if (todayCell != null)
                {
                    todayMarkerRoot.gameObject.SetActive(true);
                    todayMarkerRoot.position = todayCell.Rect.position + new Vector3(0f, 42f, 0f);
                }
                else
                {
                    todayMarkerRoot.gameObject.SetActive(false);
                }
            }
        }

        private void RefreshMonthHints()
        {
            ApplyHintVisual(hintQButton, _viewMonth > MinViewMonth);
            ApplyHintVisual(hintEButton, _viewMonth < MaxViewMonth);
        }

        private static void ApplyHintVisual(Button hint, bool enabled)
        {
            if (hint == null)
            {
                return;
            }

            hint.interactable = enabled;
            var image = hint.targetGraphic as Image ?? hint.GetComponent<Image>();
            if (image != null)
            {
                image.color = enabled
                    ? new Color(0.08f, 0.12f, 0.28f, 0.9f)
                    : new Color(0.08f, 0.12f, 0.28f, 0.28f);
            }

            var label = hint.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.color = enabled ? Color.white : new Color(1f, 1f, 1f, 0.25f);
            }
        }

        private static string BuildDayInfo(int month, int day, GameDate current, HashSet<int> eventDays)
        {
            var date = new GameDate(month, day);
            var parts = new List<string>
            {
                date.ToDisplayString()
            };

            if (date == current)
            {
                parts.Add("TODAY");
            }

            if (month == ArcMonth && day == GameDate.VaultDeadline.Day)
            {
                parts.Add("Vault deadline");
            }
            else if (eventDays.Contains(day))
            {
                parts.Add(DescribeStoryDay(day));
            }

            return string.Join("  ·  ", parts);
        }

        private static string DescribeStoryDay(int day) => day switch
        {
            1 => "Ren arrives HIMA",
            2 => "Astra tour",
            5 => "Ceremony / Resonance Dive",
            6 => "Charlotte / LUXE",
            _ => "Story beat"
        };

        private static HashSet<int> CollectEventDays(GameMetaState state)
        {
            return new HashSet<int> { 1, 2, 5, 6, GameDate.VaultDeadline.Day };
        }

        private static Button CreateKeyHint(Transform parent, string name, string key, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>(), anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            var image = go.GetComponent<Image>();
            image.color = new Color(0.08f, 0.12f, 0.28f, 0.9f);
            image.raycastTarget = true;
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            var label = CreateText(go.transform, "Label", key, 16, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            label.fontStyle = FontStyle.Bold;
            label.color = Color.white;
            return button;
        }

        private static Button CreateCloseButton(Transform parent)
        {
            var go = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            Stretch(go.GetComponent<RectTransform>(), new Vector2(0.86f, 0.02f), new Vector2(0.98f, 0.08f), Vector2.zero, Vector2.zero);
            var image = go.GetComponent<Image>();
            image.color = new Color(0.04f, 0.05f, 0.16f, 0.85f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            var label = CreateText(go.transform, "Label", "CLOSE", 18, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            label.color = FcColorTokens.Brand.Cyan;
            label.fontStyle = FontStyle.Bold;
            return button;
        }

        private static Image CreateImage(Transform parent, string name, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.raycastTarget = false;
            return image;
        }

        private static Text CreateText(Transform parent, string name, string content, int fontSize, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = Color.white;
            text.raycastTarget = false;
            UiFontCatalog.ApplyAutomatic(text);
            return text;
        }

        private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void DestroyImmediateSafe(GameObject go)
        {
            if (go == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(go);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private static Sprite LoadSprite(string fileNameNoExt)
        {
            var fromResources = Resources.Load<Sprite>($"UI/Calendar/{fileNameNoExt}");
            if (fromResources != null)
            {
                return fromResources;
            }

            var all = Resources.LoadAll<Sprite>($"UI/Calendar/{fileNameNoExt}");
            if (all != null && all.Length > 0)
            {
                return all[0];
            }

#if UNITY_EDITOR
            var artPath = $"Assets/FracturedChorus/Art/UI/Calendar/{fileNameNoExt}.png";
            var importer = AssetImporter.GetAtPath(artPath) as TextureImporter;
            if (importer != null)
            {
                var dirty = false;
                if (importer.textureType != TextureImporterType.Sprite)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    dirty = true;
                }

                if (importer.spriteImportMode != SpriteImportMode.Single)
                {
                    importer.spriteImportMode = SpriteImportMode.Single;
                    dirty = true;
                }

                if (dirty)
                {
                    importer.SaveAndReimport();
                }
            }

            var editorSprite = AssetDatabase.LoadAssetAtPath<Sprite>(artPath);
            if (editorSprite != null)
            {
                return editorSprite;
            }

            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(artPath))
            {
                if (asset is Sprite sprite)
                {
                    return sprite;
                }
            }
#endif
            return null;
        }
    }
}
