#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class LuxeArenaWireBootstrap
{
    static LuxeArenaWireBootstrap()
    {
        if (!System.Environment.CommandLine.Contains("-luxeArenaWire"))
            return;
        EditorApplication.delayCall += () =>
        {
            try
            {
                FracturedChorus.Editor.LuxeArenaBackgroundSetupEditor.WireFromBatch();
                EditorApplication.Exit(0);
            }
            catch (System.Exception ex)
            {
                UnityEngine.Debug.LogError(ex);
                EditorApplication.Exit(1);
            }
        };
    }
}
#endif
