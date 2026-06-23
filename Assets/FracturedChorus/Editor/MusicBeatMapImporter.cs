#if UNITY_EDITOR
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using FracturedChorus.Audio;
using UnityEditor;
using UnityEngine;

namespace FracturedChorus.Editor
{
    public static class MusicBeatMapImporter
    {
        private const string DefaultCsvPath = "Assets/FracturedChorus/Audio/Music/EternalSpark_CadenceRemix_beats.csv";
        private const string DefaultClipPath = "Assets/FracturedChorus/Audio/Music/EternalSpark_CadenceRemix.mp3";
        private const string DefaultAssetPath = "Assets/FracturedChorus/Audio/Music/EternalSpark_CadenceRemix_BeatMap.asset";

        [MenuItem("Fractured Chorus/Import Beat Map CSV (Cadence Remix)")]
        public static void ImportCadenceRemixBeatMap()
        {
            ImportCsvToAsset(DefaultCsvPath, DefaultClipPath, DefaultAssetPath);
        }

        [MenuItem("Fractured Chorus/Import Beat Map CSV From File...")]
        public static void ImportBeatMapFromFile()
        {
            var csvPath = EditorUtility.OpenFilePanel("Beat map CSV", Application.dataPath, "csv,txt");
            if (string.IsNullOrEmpty(csvPath))
            {
                return;
            }

            var projectRelative = ToProjectRelativePath(csvPath);
            if (string.IsNullOrEmpty(projectRelative))
            {
                EditorUtility.DisplayDialog("Import Beat Map",
                    "CSV must be inside this Unity project folder.", "OK");
                return;
            }

            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(DefaultClipPath);
            if (clip == null)
            {
                EditorUtility.DisplayDialog("Import Beat Map",
                    "Default audio clip not found. Import will continue without clip reference.", "OK");
            }

            var assetPath = EditorUtility.SaveFilePanelInProject(
                "Save Beat Map Asset",
                "MusicBeatMap",
                "asset",
                "Choose where to save the beat map asset.",
                "Assets/FracturedChorus/Audio/Music");

            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            ImportCsvToAsset(projectRelative, clip != null ? AssetDatabase.GetAssetPath(clip) : null, assetPath);
        }

        public static MusicBeatMapSO ImportCsvToAsset(string csvProjectPath, string clipProjectPath, string assetPath)
        {
            var csvFullPath = ToAbsoluteProjectPath(csvProjectPath);
            if (!File.Exists(csvFullPath))
            {
                Debug.LogError($"[BeatMap] CSV not found: {csvProjectPath}");
                return null;
            }

            var times = ParseBeatCsv(File.ReadAllText(csvFullPath));
            if (times.Count == 0)
            {
                Debug.LogError($"[BeatMap] No beat rows found in {csvProjectPath}");
                return null;
            }

            if (times[0] > 0.001f)
            {
                times.Insert(0, 0f);
            }

            var map = AssetDatabase.LoadAssetAtPath<MusicBeatMapSO>(assetPath);
            if (map == null)
            {
                map = ScriptableObject.CreateInstance<MusicBeatMapSO>();
                AssetDatabase.CreateAsset(map, assetPath);
            }

            AudioClip clip = null;
            if (!string.IsNullOrEmpty(clipProjectPath))
            {
                clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipProjectPath);
            }

            map.SetData(clip, times.ToArray());

            var music = Object.FindAnyObjectByType<CombatMusicController>();
            if (music != null)
            {
                var so = new SerializedObject(music);
                so.FindProperty("beatMap").objectReferenceValue = map;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(map);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[BeatMap] Imported {times.Count} beats from '{csvProjectPath}' → '{assetPath}'. Beat 0 @ {times[0]:F3}s.");
            Selection.activeObject = map;
            return map;
        }

        private static List<float> ParseBeatCsv(string text)
        {
            var times = new List<float>();
            var lines = text.Split('\n');
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (line.Length == 0 || line.StartsWith("#"))
                {
                    continue;
                }

                if (i == 0 && line.ToLowerInvariant().Contains("time"))
                {
                    continue;
                }

                var parts = line.Split(',');
                if (parts.Length == 1)
                {
                    parts = line.Split('\t');
                }

                if (parts.Length >= 2 && TryParseFloat(parts[1], out var sec))
                {
                    times.Add(sec);
                    continue;
                }

                if (parts.Length == 1 && TryParseFloat(parts[0], out sec))
                {
                    times.Add(sec);
                }
            }

            times.Sort();
            return times;
        }

        private static bool TryParseFloat(string raw, out float value)
        {
            return float.TryParse(raw.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        private static string ToAbsoluteProjectPath(string projectRelativePath)
        {
            if (string.IsNullOrEmpty(projectRelativePath))
            {
                return null;
            }

            projectRelativePath = projectRelativePath.Replace('\\', '/');
            if (projectRelativePath.StartsWith("Assets/"))
            {
                return Path.Combine(
                    Path.GetDirectoryName(Application.dataPath) ?? Application.dataPath,
                    projectRelativePath);
            }

            return projectRelativePath;
        }

        private static string ToProjectRelativePath(string absolutePath)
        {
            absolutePath = absolutePath.Replace('\\', '/');
            var dataPath = Application.dataPath.Replace('\\', '/');
            if (!absolutePath.StartsWith(dataPath))
            {
                return null;
            }

            return "Assets" + absolutePath.Substring(dataPath.Length);
        }
    }
}
#endif
