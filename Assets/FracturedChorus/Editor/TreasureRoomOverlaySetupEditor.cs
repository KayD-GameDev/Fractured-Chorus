#if UNITY_EDITOR
using FracturedChorus.Data;
using FracturedChorus.RunMap;
using FracturedChorus.RunMap.UI;
using FracturedChorus.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace FracturedChorus.Editor
{
    public static class TreasureRoomOverlaySetupEditor
    {
        private const string RewardFolder = "Assets/FracturedChorus/Resources/Treasure";
        private const string FlaskPath = RewardFolder + "/TreasureReward_CadenceFlask.asset";
        private const string CharmPath = RewardFolder + "/TreasureReward_MetronomeCharm.asset";
        private const string NotesPath = RewardFolder + "/TreasureReward_VaultNotes.asset";
        private const string ArtBackgroundPath = TreasureRoomOverlayUIView.BackgroundAssetPath;

        [MenuItem("Fractured Chorus/Run Map/Setup Treasure Room Overlay", false, 38)]
        public static void SetupTreasureRoomOverlay()
        {
            var table = EnsureRewardAssets();
            var canvas = FindRunMapCanvas();
            if (canvas == null)
            {
                Debug.LogError("[Fractured Chorus] RunMapCanvas not found. Open RunMapPrototype first.");
                return;
            }

            var existing = canvas.GetComponentInChildren<TreasureRoomOverlayUIView>(true);
            TreasureRoomOverlayUIView view;
            if (existing != null)
            {
                view = existing;
                Undo.RecordObject(view.gameObject, "Refresh Treasure Room Overlay");
            }
            else
            {
                var go = new GameObject("TreasureRoomOverlay", typeof(RectTransform), typeof(TreasureRoomOverlayUIView));
                Undo.RegisterCreatedObjectUndo(go, "Create Treasure Room Overlay");
                go.transform.SetParent(canvas, false);
                view = go.GetComponent<TreasureRoomOverlayUIView>();
                view.BuildDefaultHierarchy();
            }

            view.WireSceneReferences();
            view.SetRewardTable(table);
            AssignBackgroundSprite(view);
            AssignBackgroundVideo(view);
            view.ApplyBackground();
            AssignTable(view, table);

            var so = new SerializedObject(view);
            so.FindProperty("preserveSceneLayout").boolValue = true;
            so.ApplyModifiedPropertiesWithoutUndo();

            view.gameObject.SetActive(false);
            WireRunMapController(view, table);
            UiFontCatalog.ApplyHierarchy(view.transform, true);
            Selection.activeGameObject = view.gameObject;
            EditorUtility.SetDirty(view);
            EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
            Debug.Log(
                "[Fractured Chorus] TreasureRoomOverlay sẵn sàng dưới RunMapCanvas (ẩn mặc định). " +
                "Bật GameObject để chỉnh layout, Save scene.");
        }

        public static TreasureRewardTableSO EnsureRewardAssets()
        {
            if (!AssetDatabase.IsValidFolder("Assets/FracturedChorus/Resources"))
            {
                AssetDatabase.CreateFolder("Assets/FracturedChorus", "Resources");
            }

            if (!AssetDatabase.IsValidFolder(RewardFolder))
            {
                AssetDatabase.CreateFolder("Assets/FracturedChorus/Resources", "Treasure");
            }

            var flask = EnsureReward(
                FlaskPath,
                TreasureRewardSO.CadenceFlaskId,
                "Cadence Flask",
                "Bình máu. Hồi HP toàn party sau combat.",
                TreasureRewardKind.HealPotion);
            var charm = EnsureReward(
                CharmPath,
                TreasureRewardSO.MetronomeCharmId,
                "Metronome Charm",
                "Khi đặt lên board, skill đó +1 counter.",
                TreasureRewardKind.PlaceCounterPlus1);
            var notes = EnsureReward(
                NotesPath,
                TreasureRewardSO.VaultNotesId,
                "Vault Notes",
                "Nhặt Notes từ rương.",
                TreasureRewardKind.Notes);

            var table = AssetDatabase.LoadAssetAtPath<TreasureRewardTableSO>(TreasureRewardTableSO.AssetPath);
            if (table == null)
            {
                table = ScriptableObject.CreateInstance<TreasureRewardTableSO>();
                AssetDatabase.CreateAsset(table, TreasureRewardTableSO.AssetPath);
            }

            table.EditorAssign(new[] { flask, charm, notes }, 3);
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();
            return table;
        }

        private static TreasureRewardSO EnsureReward(
            string path,
            string id,
            string title,
            string description,
            TreasureRewardKind kind)
        {
            var reward = AssetDatabase.LoadAssetAtPath<TreasureRewardSO>(path);
            if (reward == null)
            {
                reward = ScriptableObject.CreateInstance<TreasureRewardSO>();
                AssetDatabase.CreateAsset(reward, path);
            }

            reward.EditorAssign(id, title, description, kind);
            EditorUtility.SetDirty(reward);
            return reward;
        }

        private static void AssignBackgroundSprite(TreasureRoomOverlayUIView view)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtBackgroundPath);
            if (sprite == null)
            {
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                    "Assets/FracturedChorus/Resources/UI/RunMap/treasure_room_bg_v1.png");
            }

            if (sprite == null)
            {
                return;
            }

            var so = new SerializedObject(view);
            so.FindProperty("backgroundSprite").objectReferenceValue = sprite;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignBackgroundVideo(TreasureRoomOverlayUIView view)
        {
            var clip = AssetDatabase.LoadAssetAtPath<VideoClip>(TreasureRoomOverlayUIView.BackgroundVideoAssetPath);
            if (clip == null)
            {
                clip = AssetDatabase.LoadAssetAtPath<VideoClip>(
                    "Assets/FracturedChorus/Resources/UI/RunMap/treasure_room_bg_v1.mp4");
            }

            if (clip == null)
            {
                return;
            }

            var so = new SerializedObject(view);
            so.FindProperty("backgroundVideo").objectReferenceValue = clip;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AssignTable(TreasureRoomOverlayUIView view, TreasureRewardTableSO table)
        {
            var so = new SerializedObject(view);
            so.FindProperty("rewardTable").objectReferenceValue = table;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireRunMapController(TreasureRoomOverlayUIView view, TreasureRewardTableSO table)
        {
            var controller = Object.FindAnyObjectByType<RunMapController>(FindObjectsInactive.Include);
            if (controller == null)
            {
                return;
            }

            var so = new SerializedObject(controller);
            so.FindProperty("treasureOverlay").objectReferenceValue = view;
            so.FindProperty("treasureRewards").objectReferenceValue = table;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
        }

        private static Transform FindRunMapCanvas()
        {
            var named = GameObject.Find("RunMapCanvas");
            if (named != null)
            {
                return named.transform;
            }

            var canvas = Object.FindAnyObjectByType<Canvas>();
            return canvas != null ? canvas.transform : null;
        }
    }
}
#endif
