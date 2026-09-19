namespace FracturedChorus.Hub
{
    public static class HubNavigationEscContext
    {
        public enum ReturnMode
        {
            TownMap = 0,
            StatusMenu = 1,
            StatusMenuSystemSubmenu = 2
        }

        private static ReturnMode s_mode = ReturnMode.TownMap;
        private static MetaStatusMenuUI.Tab s_tab = MetaStatusMenuUI.Tab.Stats;
        private static bool s_active;

        public static void SetReturnToTownMap()
        {
            s_mode = ReturnMode.TownMap;
            s_active = true;
        }

        public static void SetReturnToStatusMenu(MetaStatusMenuUI.Tab tab = MetaStatusMenuUI.Tab.Stats)
        {
            s_mode = ReturnMode.StatusMenu;
            s_tab = tab;
            s_active = true;
        }

        public static void SetReturnToSystemSubmenu()
        {
            s_mode = ReturnMode.StatusMenuSystemSubmenu;
            s_tab = MetaStatusMenuUI.Tab.Stats;
            s_active = true;
        }

        public static void Clear()
        {
            s_active = false;
        }

        public static bool HasPending => s_active;

        public static MetaStatusMenuUI.Tab PendingTab => s_tab;

        public static bool WantsStatusMenuReopen =>
            s_active && s_mode != ReturnMode.TownMap;

        public static void ApplyBeforeLoadCampusHub(bool forceTownMap = false)
        {
            if (forceTownMap || !s_active || s_mode == ReturnMode.TownMap)
            {
                TownMapView.PrepareReturnToHub(false);
                return;
            }

            if (s_mode == ReturnMode.StatusMenuSystemSubmenu)
            {
                TownMapView.PrepareReturnToHub(true, MetaStatusMenuUI.Tab.Stats);
                TownMapView.OpenSystemSubmenuOnNextShow = true;
                return;
            }

            TownMapView.PrepareReturnToHub(true, s_tab);
        }

        public static bool ConsumePending(out ReturnMode mode, out MetaStatusMenuUI.Tab tab)
        {
            mode = s_mode;
            tab = s_tab;
            var had = s_active;
            s_active = false;
            return had;
        }

        public static void MarkRunMapOpenedFromStatusMenu(MetaStatusMenuUI.Tab tab = MetaStatusMenuUI.Tab.Stats)
        {
            SetReturnToStatusMenu(tab);
        }

        public static void MarkRunMapOpenedFromTownActivity()
        {
            SetReturnToTownMap();
        }

        public static bool ConsumeReopenAfterRunMapEsc(out MetaStatusMenuUI.Tab tab)
        {
            tab = s_tab;
            var reopen = WantsStatusMenuReopen;
            s_active = false;
            return reopen;
        }

        public static bool HasPendingReopenAfterRunMapEsc => WantsStatusMenuReopen;
    }
}
