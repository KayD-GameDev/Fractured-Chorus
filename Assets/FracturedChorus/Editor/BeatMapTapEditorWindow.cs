#if UNITY_EDITOR
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using FracturedChorus.Audio;
using UnityEditor;
using UnityEngine;

namespace FracturedChorus.Editor
{
    public class BeatMapTapEditorWindow : EditorWindow
    {
        private const string DefaultClipPath = "Assets/FracturedChorus/Audio/Music/EternalSpark_CadenceRemix.mp3";
        private const string DefaultCsvPath = "Assets/FracturedChorus/Audio/Music/EternalSpark_CadenceRemix_beats.csv";

        private AudioClip _clip;
        private TextAsset _csvToLoad;
        private string _exportPath = DefaultCsvPath;

        private readonly List<float> _beatTimes = new();
        private float _scrubTime;
        private bool _isPlaying;
        private double _playStartEditorTime;
        private float _playStartAudioTime;

        [MenuItem("Fractured Chorus/Beat Map Tap Editor")]
        public static void Open()
        {
            var window = GetWindow<BeatMapTapEditorWindow>("Beat Map Tap");
            window.minSize = new Vector2(420f, 360f);
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;

            if (_clip == null)
            {
                _clip = AssetDatabase.LoadAssetAtPath<AudioClip>(DefaultClipPath);
            }

            if (_beatTimes.Count == 0)
            {
                LoadCsvFromProjectPath(DefaultCsvPath, silent: true);
            }
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            StopPreview();
        }

        private void OnEditorUpdate()
        {
            if (!_isPlaying || _clip == null)
            {
                return;
            }

            var t = GetPlaybackTime();
            if (t >= _clip.length)
            {
                StopPreview();
            }

            Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Beat Map Tap Editor", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Play nhạc → bấm Space mỗi kick / vocal peak / nhấn nhạc.\n" +
                "Đoạn yên: không bấm → beat tự thưa.\n" +
                "Backspace: xóa beat cuối. Ctrl+S: lưu CSV.",
                MessageType.Info);

            EditorGUI.BeginChangeCheck();
            _clip = (AudioClip)EditorGUILayout.ObjectField("Audio Clip", _clip, typeof(AudioClip), false);
            _csvToLoad = (TextAsset)EditorGUILayout.ObjectField("Load CSV", _csvToLoad, typeof(TextAsset), false);
            _exportPath = EditorGUILayout.TextField("Export CSV Path", _exportPath);
            if (EditorGUI.EndChangeCheck() && _csvToLoad != null)
            {
                LoadCsvText(_csvToLoad.text);
            }

            EditorGUILayout.Space(8f);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Load Default CSV"))
                {
                    LoadCsvFromProjectPath(DefaultCsvPath);
                }

                if (GUILayout.Button("Clear All"))
                {
                    if (EditorUtility.DisplayDialog("Clear beats", "Xóa toàn bộ beat đã ghi?", "Clear", "Cancel"))
                    {
                        _beatTimes.Clear();
                    }
                }
            }

            EditorGUILayout.Space(4f);
            DrawTransport();
            EditorGUILayout.Space(4f);
            DrawBeatList();
            EditorGUILayout.Space(8f);
            DrawExport();

            HandleShortcuts();
        }

        private void DrawTransport()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Transport", EditorStyles.miniBoldLabel);

                var maxTime = _clip != null ? _clip.length : 0f;
                var displayTime = _isPlaying ? GetPlaybackTime() : _scrubTime;
                displayTime = Mathf.Clamp(displayTime, 0f, maxTime);

                EditorGUI.BeginChangeCheck();
                _scrubTime = EditorGUILayout.Slider("Time (sec)", displayTime, 0f, maxTime);
                if (EditorGUI.EndChangeCheck() && !_isPlaying)
                {
                    // scrub when stopped
                }

                EditorGUILayout.LabelField("Display", FormatTime(displayTime));
                EditorGUILayout.LabelField("Beat count", _beatTimes.Count.ToString());

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUI.enabled = _clip != null && !_isPlaying;
                    if (GUILayout.Button("Play", GUILayout.Height(28f)))
                    {
                        StartPreview(_scrubTime);
                    }

                    GUI.enabled = _isPlaying;
                    if (GUILayout.Button("Stop", GUILayout.Height(28f)))
                    {
                        StopPreview();
                    }

                    GUI.enabled = true;
                }

                EditorGUILayout.LabelField("Shortcuts",
                    "Space = tap beat | Backspace = undo | Ctrl+S = save CSV",
                    EditorStyles.miniLabel);
            }
        }

        private void DrawBeatList()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Recent beats", EditorStyles.miniBoldLabel);
                if (_beatTimes.Count == 0)
                {
                    EditorGUILayout.LabelField("(empty — Play rồi bấm Space)");
                    return;
                }

                var start = Mathf.Max(0, _beatTimes.Count - 8);
                for (var i = start; i < _beatTimes.Count; i++)
                {
                    var gap = i > 0 ? _beatTimes[i] - _beatTimes[i - 1] : _beatTimes[i];
                    EditorGUILayout.LabelField(
                        $"beat {i}: {FormatTime(_beatTimes[i])}  (gap {(i > 0 ? gap.ToString("F3") : "—")}s)");
                }
            }
        }

        private void DrawExport()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Export CSV", GUILayout.Height(30f)))
                {
                    ExportCsv();
                }

                if (GUILayout.Button("Import To Beat Map Asset", GUILayout.Height(30f)))
                {
                    ExportCsv();
                    MusicBeatMapImporter.ImportCsvToAsset(
                        _exportPath,
                        _clip != null ? AssetDatabase.GetAssetPath(_clip) : DefaultClipPath,
                        "Assets/FracturedChorus/Audio/Music/EternalSpark_CadenceRemix_BeatMap.asset");
                }
            }
        }

        private void HandleShortcuts()
        {
            var e = Event.current;
            if (e.type != EventType.KeyDown || e.alt)
            {
                return;
            }

            if (e.control && e.keyCode == KeyCode.S)
            {
                ExportCsv();
                e.Use();
                return;
            }

            if (e.keyCode == KeyCode.Backspace)
            {
                UndoLastBeat();
                e.Use();
                return;
            }

            if (e.keyCode == KeyCode.Space)
            {
                if (GUI.GetNameOfFocusedControl() == "Export CSV Path")
                {
                    return;
                }

                TapBeat();
                e.Use();
            }
        }

        private void TapBeat()
        {
            if (_clip == null)
            {
                ShowNotification(new GUIContent("Assign Audio Clip first."));
                return;
            }

            var t = _isPlaying ? GetPlaybackTime() : _scrubTime;
            t = Mathf.Clamp(t, 0f, _clip.length);

            if (_beatTimes.Count > 0 && t <= _beatTimes[_beatTimes.Count - 1] + 0.02f)
            {
                ShowNotification(new GUIContent("Beat quá gần beat trước — bỏ qua."));
                return;
            }

            _beatTimes.Add(t);
            Debug.Log($"[BeatTap] beat {_beatTimes.Count - 1} @ {t:F4}s");
            Repaint();
        }

        private void UndoLastBeat()
        {
            if (_beatTimes.Count == 0)
            {
                return;
            }

            var removed = _beatTimes[_beatTimes.Count - 1];
            _beatTimes.RemoveAt(_beatTimes.Count - 1);
            Debug.Log($"[BeatTap] Removed beat @ {removed:F4}s");
            Repaint();
        }

        private void StartPreview(float fromTime)
        {
            if (_clip == null)
            {
                return;
            }

            StopPreview();
            _playStartEditorTime = EditorApplication.timeSinceStartup;
            _playStartAudioTime = Mathf.Clamp(fromTime, 0f, _clip.length);
            _scrubTime = _playStartAudioTime;
            _isPlaying = true;

            var startSample = Mathf.Clamp(
                (int)(_playStartAudioTime * _clip.frequency),
                0,
                Mathf.Max(0, _clip.samples - 1));

            EditorPreviewAudio.PlayPreviewClip(_clip, startSample, false);
        }

        private void StopPreview()
        {
            if (!_isPlaying)
            {
                return;
            }

            _scrubTime = GetPlaybackTime();
            _isPlaying = false;
            EditorPreviewAudio.StopAllPreviewClips();
        }

        private float GetPlaybackTime()
        {
            if (!_isPlaying)
            {
                return _scrubTime;
            }

            var elapsed = (float)(EditorApplication.timeSinceStartup - _playStartEditorTime);
            return Mathf.Min(_playStartAudioTime + elapsed, _clip != null ? _clip.length : 0f);
        }

        private void LoadCsvFromProjectPath(string projectPath, bool silent = false)
        {
            var full = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? Application.dataPath, projectPath);
            if (!File.Exists(full))
            {
                if (!silent)
                {
                    Debug.LogWarning($"[BeatTap] CSV not found: {projectPath}");
                }

                return;
            }

            LoadCsvText(File.ReadAllText(full));
            if (!silent)
            {
                Debug.Log($"[BeatTap] Loaded {_beatTimes.Count} beats from {projectPath}");
            }
        }

        private void LoadCsvText(string csvText)
        {
            _beatTimes.Clear();
            var times = MusicBeatMapSO.ParseCsvTimes(csvText);
            _beatTimes.AddRange(times);
            Repaint();
        }

        private void ExportCsv()
        {
            if (_beatTimes.Count == 0)
            {
                EditorUtility.DisplayDialog("Export CSV", "Chưa có beat nào để export.", "OK");
                return;
            }

            var sorted = _beatTimes.OrderBy(t => t).ToList();
            var lines = new List<string> { "beat,time_sec" };
            for (var i = 0; i < sorted.Count; i++)
            {
                lines.Add($"{i},{sorted[i].ToString("F4", CultureInfo.InvariantCulture)}");
            }

            var projectRelative = _exportPath.Replace('\\', '/');
            var fullPath = projectRelative.StartsWith("Assets/")
                ? Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? Application.dataPath, projectRelative)
                : projectRelative;

            var dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            File.WriteAllText(fullPath, string.Join("\n", lines));
            AssetDatabase.Refresh();

            Debug.Log($"[BeatTap] Exported {sorted.Count} beats → {projectRelative}");
            ShowNotification(new GUIContent($"Saved {sorted.Count} beats"));
        }

        private static string FormatTime(float seconds)
        {
            var m = Mathf.FloorToInt(seconds / 60f);
            var s = seconds % 60f;
            return $"{m}:{s:00.000}";
        }
    }
}
#endif
