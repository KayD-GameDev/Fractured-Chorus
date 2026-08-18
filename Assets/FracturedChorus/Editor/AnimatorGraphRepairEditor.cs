#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FracturedChorus.Editor
{
    /// <summary>
    /// UnityEditor.Graphs.Edge.WakeUp NRE is from a stale Animator window, not Play Mode.
    /// Close that window. Do not FindObjectsOfTypeAll(Edge) — Edge is not a UnityEngine.Object.
    /// </summary>
    internal static class AnimatorGraphRepairEditor
    {
        [MenuItem("Fractured Chorus/Repair Animator Graphs (close stale window)")]
        public static void RepairFromMenu()
        {
            Repair(log: true);
        }

        private static void Repair(bool log)
        {
            var closed = CloseStaleAnimatorGraphWindows();
            var destroyed = DestroyStaleGraphObjects();
            if (log)
            {
                Debug.Log(
                    $"[Fractured Chorus] Closed {closed} Animator window(s), " +
                    $"destroyed {destroyed} stale Graph object(s). " +
                    "Reopen Animator only after this. The old Edge.WakeUp NRE is editor-only.");
            }
        }

        private static int CloseStaleAnimatorGraphWindows()
        {
            var closed = 0;
            var windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
            if (windows == null)
            {
                return 0;
            }

            foreach (var window in windows)
            {
                if (window == null)
                {
                    continue;
                }

                var typeName = window.GetType().FullName ?? string.Empty;
                if (!IsAnimatorGraphWindow(typeName))
                {
                    continue;
                }

                window.Close();
                closed++;
            }

            return closed;
        }

        private static bool IsAnimatorGraphWindow(string typeName)
        {
            return typeName.IndexOf("AnimatorControllerTool", StringComparison.Ordinal) >= 0
                   || typeName.IndexOf("UnityEditor.Graphs.GraphGUI", StringComparison.Ordinal) >= 0;
        }

        private static int DestroyStaleGraphObjects()
        {
            var graphType = FindType("UnityEditor.Graphs.Graph");
            if (graphType == null || !typeof(UnityEngine.Object).IsAssignableFrom(graphType))
            {
                return 0;
            }

            UnityEngine.Object[] objects;
            try
            {
                objects = Resources.FindObjectsOfTypeAll(graphType);
            }
            catch (Exception)
            {
                return 0;
            }

            if (objects == null)
            {
                return 0;
            }

            var count = 0;
            foreach (var obj in objects)
            {
                if (obj == null)
                {
                    continue;
                }

                UnityEngine.Object.DestroyImmediate(obj);
                count++;
            }

            return count;
        }

        private static Type FindType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type;
                try
                {
                    type = assembly.GetType(fullName);
                }
                catch (ReflectionTypeLoadException)
                {
                    continue;
                }

                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }
    }
}
#endif
