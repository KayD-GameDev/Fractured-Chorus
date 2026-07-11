using FracturedChorus.Meta;
using UnityEngine;

namespace FracturedChorus.Hub
{
    public sealed class UiEditPreviewRoot : MonoBehaviour
    {
        public enum PreviewMode
        {
            StatusMenu = 0,
            Calendar = 1
        }

        [SerializeField] private PreviewMode mode = PreviewMode.Calendar;
        [SerializeField] private GameObject statusMenuRoot;
        [SerializeField] private GameObject calendarRoot;
        [SerializeField] private MetaStatusMenuUI statusMenu;
        [SerializeField] private CalendarOverlayUI calendarOverlay;
        [SerializeField] private bool applyMockDataOnEnable = true;

        [Header("Layer refs — Status")]
        [SerializeField] private GameObject statusBackground;
        [SerializeField] private GameObject statusDateChip;
        [SerializeField] private GameObject statusMenuList;
        [SerializeField] private GameObject statusDetailPanel;
        [SerializeField] private GameObject statusTooltip;
        [SerializeField] private GameObject statusPrompts;

        [Header("Layer refs — Calendar")]
        [SerializeField] private GameObject calendarBackground;
        [SerializeField] private GameObject calendarLeftPanel;
        [SerializeField] private GameObject calendarTitleYear;
        [SerializeField] private GameObject calendarMonthBlock;
        [SerializeField] private GameObject calendarWeekdayRow;
        [SerializeField] private GameObject calendarDayGrid;
        [SerializeField] private GameObject calendarSelectedInfo;
        [SerializeField] private GameObject calendarDateChip;
        [SerializeField] private GameObject calendarTodayMarker;
        [SerializeField] private GameObject calendarClose;

        private void OnEnable()
        {
            ApplyMode();
            if (applyMockDataOnEnable)
            {
                RefreshMockData();
            }
        }

        private void OnValidate()
        {
            ApplyMode();
        }

        public void SetMode(PreviewMode previewMode)
        {
            mode = previewMode;
            ApplyMode();
            RefreshMockData();
        }

        public void ApplyMode()
        {
            if (statusMenuRoot != null)
            {
                statusMenuRoot.SetActive(mode == PreviewMode.StatusMenu);
            }

            if (calendarRoot != null)
            {
                calendarRoot.SetActive(mode == PreviewMode.Calendar);
            }
        }

        public void RefreshMockData()
        {
            var state = GameMetaState.CreateHubStart();
            state.Calendar.CurrentDate = new GameDate(9, 12);
            state.Calendar.CurrentPhase = DayPhase.Day;
            state.Flags.SetBool(StoryFlagIds.VaultQuestActive, true);

            if (mode == PreviewMode.StatusMenu && statusMenu != null)
            {
                statusMenu.Show(state, MetaStatusMenuUI.Tab.Stats);
            }

            if (mode == PreviewMode.Calendar && calendarOverlay != null)
            {
                calendarOverlay.Show(state);
            }
        }

        public void BindLayerRefs(
            GameObject statusRoot,
            MetaStatusMenuUI status,
            GameObject calendar,
            CalendarOverlayUI overlay)
        {
            statusMenuRoot = statusRoot;
            statusMenu = status;
            calendarRoot = calendar;
            calendarOverlay = overlay;

            if (statusRoot != null)
            {
                statusBackground = FindChild(statusRoot.transform, "L00_Background", "Background");
                statusDateChip = FindChild(statusRoot.transform, "L01_DateChip", "DateChip");
                statusMenuList = FindChild(statusRoot.transform, "L02_MenuList", "MenuList");
                statusDetailPanel = FindChild(statusRoot.transform, "L03_DetailPanel", "DetailPanel");
                statusTooltip = FindChild(statusRoot.transform, "L04_Tooltip", "Tooltip");
                statusPrompts = FindChild(statusRoot.transform, "L05_Prompts", "Prompts");
            }

            if (calendar != null)
            {
                calendarBackground = FindChild(calendar.transform, "L00_Background", "Background");
                calendarLeftPanel = FindChild(calendar.transform, "L01_LeftPanel", "LeftPanel");
                calendarDateChip = FindChild(calendar.transform, "L05_DateChip", "DateChipBg");
                calendarTodayMarker = FindChild(calendar.transform, "L06_TodayMarker", "TodayMarker");
                calendarClose = FindChild(calendar.transform, "L07_Close", "CloseButton");

                if (calendarLeftPanel != null)
                {
                    var left = calendarLeftPanel.transform;
                    calendarTitleYear = FindChild(left, "L01a_Title", "Title");
                    calendarMonthBlock = FindChild(left, "L02a_MonthBig", "MonthBig");
                    calendarWeekdayRow = FindChild(left, "L03_WeekdayRow", "WeekdayRow");
                    calendarDayGrid = FindChild(left, "L04_DayGrid", "DayGrid");
                    calendarSelectedInfo = FindChild(left, "L04b_SelectedDayInfo", "SelectedDayInfo");
                }
            }
        }

        private static GameObject FindChild(Transform parent, params string[] names)
        {
            foreach (var name in names)
            {
                var child = parent.Find(name);
                if (child != null)
                {
                    return child.gameObject;
                }
            }

            return null;
        }
    }
}
