#if UNITY_EDITOR
using System.IO;
using FracturedChorus.Meta;
using UnityEditor;
using UnityEngine;

namespace FracturedChorus.Editor
{
    /// <summary>
    /// Bảng điều khiển save cho dev: xem 10 slot, xuất/nhập JSON đã giải mã, khôi phục backup đời cũ.
    /// Save trong build là file mã hóa nên không thể mở bằng text editor — đây là đường vào chính thức.
    /// </summary>
    public sealed class SaveSystemEditorWindow : EditorWindow
    {
        private SaveSlotHeader[] _headers = System.Array.Empty<SaveSlotHeader>();
        private Vector2 _scroll;

        [MenuItem("Fractured Chorus/Save/Save Slots Manager")]
        public static void Open()
        {
            var window = GetWindow<SaveSystemEditorWindow>(false, "Save Slots");
            window.minSize = new Vector2(520f, 400f);
            window.Refresh();
        }

        [MenuItem("Fractured Chorus/Save/Open Saves Folder")]
        public static void OpenSavesFolder()
        {
            Directory.CreateDirectory(GameMetaSaveLoad.SavesDirectory);
            EditorUtility.RevealInFinder(GameMetaSaveLoad.SavesDirectory);
        }

        [MenuItem("Fractured Chorus/Save/Restore From Legacy Backup")]
        public static void RestoreFromLegacyBackup()
        {
            if (!Directory.Exists(GameMetaSaveLoad.LegacyBackupDirectory))
            {
                EditorUtility.DisplayDialog(
                    "Restore From Legacy Backup",
                    $"Không tìm thấy thư mục backup:\n{GameMetaSaveLoad.LegacyBackupDirectory}",
                    "OK");
                return;
            }

            var confirmed = EditorUtility.DisplayDialog(
                "Restore From Legacy Backup",
                "Kéo các save JSON đời cũ trong _legacy_v2/ về lại slot?\n\n" +
                "Slot nào có backup sẽ bị GHI ĐÈ bằng dữ liệu cũ.",
                "Khôi phục",
                "Hủy");

            if (!confirmed)
            {
                return;
            }

            var restored = GameMetaSaveLoad.RestoreFromLegacyBackup();
            EditorUtility.DisplayDialog(
                "Restore From Legacy Backup",
                restored > 0 ? $"Đã khôi phục {restored} slot." : "Không có slot nào khôi phục được.",
                "OK");
        }

        private void OnEnable() => Refresh();

        private void Refresh()
        {
            _headers = GameMetaSaveLoad.ListHeaders();
        }

        private void OnGUI()
        {
            DrawToolbar();
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Thư mục save", GameMetaSaveLoad.SavesDirectory, EditorStyles.miniLabel);
            EditorGUILayout.Space(6f);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            for (var slot = 0; slot < _headers.Length; slot++)
            {
                DrawSlotRow(slot, _headers[slot]);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70f)))
            {
                Refresh();
            }

            if (GUILayout.Button("Open Folder", EditorStyles.toolbarButton, GUILayout.Width(90f)))
            {
                OpenSavesFolder();
            }

            if (GUILayout.Button("Export All", EditorStyles.toolbarButton, GUILayout.Width(80f)))
            {
                ExportAllSlots();
            }

            GUILayout.FlexibleSpace();

            var plaintext = GUILayout.Toggle(
                SaveCrypto.DebugPlaintext,
                "Ghi save plaintext",
                EditorStyles.toolbarButton,
                GUILayout.Width(140f));

            if (plaintext != SaveCrypto.DebugPlaintext)
            {
                SaveCrypto.DebugPlaintext = plaintext;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawSlotRow(int slot, SaveSlotHeader header)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField($"SLOT {slot + 1:00}", EditorStyles.boldLabel, GUILayout.Width(70f));
            EditorGUILayout.LabelField(DescribeSlot(header));

            using (new EditorGUI.DisabledScope(header.isEmpty))
            {
                if (GUILayout.Button("Export", GUILayout.Width(64f)))
                {
                    ExportSlot(slot);
                }
            }

            if (GUILayout.Button("Import", GUILayout.Width(64f)))
            {
                ImportSlot(slot);
            }

            using (new EditorGUI.DisabledScope(header.isEmpty))
            {
                if (GUILayout.Button("Delete", GUILayout.Width(64f)))
                {
                    DeleteSlot(slot);
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private static string DescribeSlot(SaveSlotHeader header)
        {
            if (header.isCorrupted)
            {
                return "HỎNG — sai chữ ký hoặc không giải mã được";
            }

            if (header.isEmpty)
            {
                return "Trống";
            }

            return
                $"{header.dateMonth:00}/{header.dateDay:00} · {header.locationLabel} · " +
                $"{header.notes} notes · playtime {header.FormatPlayTime()}";
        }

        private void ExportSlot(int slot)
        {
            var json = GameMetaSaveLoad.ExportSlotJson(slot);
            if (string.IsNullOrEmpty(json))
            {
                EditorUtility.DisplayDialog("Export Slot", $"Slot {slot + 1:00} không đọc được.", "OK");
                return;
            }

            var path = EditorUtility.SaveFilePanel(
                "Export save slot",
                GameMetaSaveLoad.SavesDirectory,
                $"slot_{slot:00}_export.json",
                "json");

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            File.WriteAllText(path, json);
            Debug.Log($"[Fractured Chorus] Đã xuất slot {slot + 1:00} ra {path}");
        }

        private void ExportAllSlots()
        {
            var folder = EditorUtility.OpenFolderPanel("Export tất cả slot", GameMetaSaveLoad.SavesDirectory, string.Empty);
            if (string.IsNullOrEmpty(folder))
            {
                return;
            }

            var exported = 0;
            for (var slot = 0; slot < GameMetaSaveLoad.SlotCount; slot++)
            {
                var json = GameMetaSaveLoad.ExportSlotJson(slot);
                if (string.IsNullOrEmpty(json))
                {
                    continue;
                }

                File.WriteAllText(Path.Combine(folder, $"slot_{slot:00}_export.json"), json);
                exported++;
            }

            EditorUtility.DisplayDialog("Export All", $"Đã xuất {exported} slot.", "OK");
        }

        private void ImportSlot(int slot)
        {
            var path = EditorUtility.OpenFilePanel("Import save JSON", GameMetaSaveLoad.SavesDirectory, "json");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var confirmed = EditorUtility.DisplayDialog(
                "Import Slot",
                $"Ghi đè SLOT {slot + 1:00} bằng nội dung file này?\n\n{path}",
                "Ghi đè",
                "Hủy");

            if (!confirmed)
            {
                return;
            }

            if (GameMetaSaveLoad.ImportSlotJson(slot, File.ReadAllText(path)))
            {
                Debug.Log($"[Fractured Chorus] Đã nhập {path} vào slot {slot + 1:00}");
                Refresh();
            }
            else
            {
                EditorUtility.DisplayDialog("Import Slot", "JSON không hợp lệ — slot giữ nguyên.", "OK");
            }
        }

        private void DeleteSlot(int slot)
        {
            var confirmed = EditorUtility.DisplayDialog(
                "Delete Slot",
                $"Xóa hẳn SLOT {slot + 1:00}?",
                "Xóa",
                "Hủy");

            if (!confirmed)
            {
                return;
            }

            GameMetaSaveLoad.Delete(slot);
            Refresh();
        }
    }
}
#endif
