#if UNITY_EDITOR
using FracturedChorus.Data;
using UnityEngine;

namespace FracturedChorus.Editor
{
    internal static class CadenceMapMaskEditSession
    {
        public static bool PreviewEnabled { get; set; }
        public static CadenceMapLayoutSO Layout { get; set; }
        public static int SelectedTerritory { get; set; }
        public static int SelectedVertex { get; set; } = -1;
        public static System.Action LayoutChanged { get; set; }

        public static void NotifyLayoutChanged()
        {
            LayoutChanged?.Invoke();
        }

        public static void Reset()
        {
            PreviewEnabled = false;
            Layout = null;
            SelectedTerritory = 0;
            SelectedVertex = -1;
            LayoutChanged = null;
        }
    }
}
#endif
