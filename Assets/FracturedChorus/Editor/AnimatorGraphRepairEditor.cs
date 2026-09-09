#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FracturedChorus.Editor
{
    internal static class AnimatorGraphRepairEditor
    {
        private const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static bool _repairQueued;

        static AnimatorGraphRepairEditor()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            CompilationPipeline.compilationStarted -= OnCompilationStarted;
            CompilationPipeline.compilationStarted += OnCompilationStarted;
            Application.logMessageReceived -= OnEditorLog;
            Application.logMessageReceived += OnEditorLog;

            var stale = SanitizeGraphEdges();
            if (stale > 0)
            {
                CloseAnimatorWindows();
            }
        }

        public static void RepairFromMenu()
        {
            var stale = SanitizeGraphEdges();
            var closed = CloseAnimatorWindows();
            var destroyed = DestroyStaleGraphObjects();
            Debug.Log(
                $"[Fractured Chorus] Removed {stale} stale Animator edge(s), " +
                $"closed {closed} Animator window(s), destroyed {destroyed} Graph object(s). " +
                "Edge.WakeUp is editor-only. Reopen Animator, or Window → Layouts → Default. " +
                "Clear Console to dismiss the old exception.");
        }

        private static int SanitizeGraphEdges()
        {
            var graphType = FindType("UnityEditor.Graphs.Graph");
            if (graphType == null || !typeof(UnityEngine.Object).IsAssignableFrom(graphType))
            {
                return 0;
            }

            UnityEngine.Object[] graphs;
            try
            {
                graphs = Resources.FindObjectsOfTypeAll(graphType);
            }
            catch (Exception)
            {
                return 0;
            }

            if (graphs == null)
            {
                return 0;
            }

            var removed = 0;
            foreach (var graph in graphs)
            {
                if (graph == null)
                {
                    continue;
                }

                removed += RemoveInvalidEdges(graph, graphType);
            }

            return removed;
        }

        private static int RemoveInvalidEdges(UnityEngine.Object graph, Type graphType)
        {
            var edges = GetEdgesList(graph, graphType);
            if (edges == null || edges.Count == 0)
            {
                return 0;
            }

            var removed = 0;
            for (var i = edges.Count - 1; i >= 0; i--)
            {
                var edge = edges[i];
                if (edge != null && IsEdgeValid(edge))
                {
                    continue;
                }

                edges.RemoveAt(i);
                removed++;
            }

            return removed;
        }

        private static IList GetEdgesList(object graph, Type graphType)
        {
            var property = graphType.GetProperty("edges", InstanceFlags);
            if (property != null)
            {
                return property.GetValue(graph) as IList;
            }

            var field = graphType.GetField("m_Edges", InstanceFlags)
                        ?? graphType.GetField("edges", InstanceFlags);
            return field == null ? null : field.GetValue(graph) as IList;
        }

        private static bool IsEdgeValid(object edge)
        {
            var fromSlot = GetMemberValue(edge, "fromSlot") ?? GetMemberValue(edge, "m_FromSlot");
            var toSlot = GetMemberValue(edge, "toSlot") ?? GetMemberValue(edge, "m_ToSlot");
            if (fromSlot == null || toSlot == null)
            {
                return false;
            }

            var fromNode = GetMemberValue(fromSlot, "node") ?? GetMemberValue(fromSlot, "m_Node");
            var toNode = GetMemberValue(toSlot, "node") ?? GetMemberValue(toSlot, "m_Node");
            return fromNode != null && toNode != null;
        }

        private static object GetMemberValue(object target, string name)
        {
            if (target == null)
            {
                return null;
            }

            var type = target.GetType();
            var property = type.GetProperty(name, InstanceFlags);
            if (property != null)
            {
                try
                {
                    return property.GetValue(target);
                }
                catch
                {
                    return null;
                }
            }

            var field = type.GetField(name, InstanceFlags);
            if (field == null)
            {
                return null;
            }

            try
            {
                return field.GetValue(target);
            }
            catch
            {
                return null;
            }
        }

        private static int CloseAnimatorWindows()
        {
            var closed = 0;
            var windows = Resources.FindObjectsOfTypeAll<EditorWindow>();
            if (windows == null)
            {
                return 0;
            }

            foreach (var window in windows)
            {
                if (!window)
                {
                    continue;
                }

                var typeName = window.GetType().FullName ?? string.Empty;
                if (!IsAnimatorGraphWindow(typeName))
                {
                    continue;
                }

                try
                {
                    window.Close();
                    closed++;
                }
                catch (Exception)
                {
                }
            }

            return closed;
        }

        private static bool IsAnimatorGraphWindow(string typeName)
        {
            return typeName.IndexOf("AnimatorControllerTool", StringComparison.Ordinal) >= 0
                   || typeName.IndexOf("UnityEditor.Graphs.GraphGUI", StringComparison.Ordinal) >= 0
                   || typeName.IndexOf("AnimationStateMachine", StringComparison.Ordinal) >= 0;
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
