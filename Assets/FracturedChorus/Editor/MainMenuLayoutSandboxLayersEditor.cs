#if UNITY_EDITOR
using FracturedChorus.Menu;
using UnityEditor;
using UnityEngine;

namespace FracturedChorus.Editor
{
    [CustomEditor(typeof(MainMenuLayoutSandboxLayers))]
    public sealed class MainMenuLayoutSandboxLayersEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var layers = (MainMenuLayoutSandboxLayers)target;
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("Layers", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Attract") && layers.AttractLayer != null)
            {
                Undo.RecordObject(layers.AttractLayer, "Preview Attract");
                if (layers.MainMenuLayer != null)
                {
                    Undo.RecordObject(layers.MainMenuLayer, "Preview Attract");
                }
                layers.ShowAttract();
                EditorUtility.SetDirty(layers);
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Main Menu") && layers.MainMenuLayer != null)
            {
                if (layers.AttractLayer != null)
                {
                    Undo.RecordObject(layers.AttractLayer, "Preview Main Menu");
                }
                Undo.RecordObject(layers.MainMenuLayer, "Preview Main Menu");
                layers.ShowMainMenu();
                EditorUtility.SetDirty(layers);
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Both"))
            {
                if (layers.AttractLayer != null)
                {
                    Undo.RecordObject(layers.AttractLayer, "Preview Both");
                }

                if (layers.MainMenuLayer != null)
                {
                    Undo.RecordObject(layers.MainMenuLayer, "Preview Both");
                }

                layers.ShowBoth();
                EditorUtility.SetDirty(layers);
                SceneView.RepaintAll();
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif
