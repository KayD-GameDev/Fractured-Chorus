#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FracturedChorus.Editor
{
    [InitializeOnLoad]
    public static class PcPlayerBuild
    {
        private const string RequestFlagName = "request-windows-build.flag";

        static PcPlayerBuild()
        {
            EditorApplication.delayCall += ConsumeBuildRequest;
        }

        private static void ConsumeBuildRequest()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += ConsumeBuildRequest;
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += ConsumeBuildRequest;
                return;
            }

            var flag = Path.Combine(Path.GetFullPath(Path.Combine(Application.dataPath, "..")), "Builds", RequestFlagName);
            if (!File.Exists(flag))
            {
                return;
            }

            File.Delete(flag);
            try
            {
                BuildWindows64();
            }
            catch (Exception error)
            {
                Debug.LogError($"[Fractured Chorus] Windows build failed: {error}");
            }
        }

        public static void BuildWindows64()
        {
            AstraPrepSaveBuilder.WriteBundledSave();
            AssetDatabase.Refresh();

            var scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            if (scenes.Length == 0)
            {
                throw new InvalidOperationException("EditorBuildSettings không có scene nào được bật.");
            }

            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var output = Path.Combine(projectRoot, "Builds", "Windows", "Fractured Chorus.exe");
            var outputDir = Path.GetDirectoryName(output);
            if (!string.IsNullOrEmpty(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Windows build failed: {report.summary.result} ({report.summary.totalErrors} errors).");
            }

            Debug.Log($"[Fractured Chorus] Windows build: {output}");
        }
    }
}
#endif
