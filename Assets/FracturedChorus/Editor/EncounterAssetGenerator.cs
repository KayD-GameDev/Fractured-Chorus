#if UNITY_EDITOR
using FracturedChorus.Combat.Bootstrap;
using FracturedChorus.Combat.Grid;
using FracturedChorus.Data;
using UnityEditor;
using UnityEngine;

namespace FracturedChorus.Editor
{
    public static class EncounterAssetGenerator
    {
        private const string Folder = "Assets/FracturedChorus/Resources/Encounters";

        [MenuItem("Fractured Chorus/Create Encounter Assets (Battle / Elite / Boss)")]
        public static void CreateEncounterAssets()
        {
            EnsureFolder(Folder);

            var grunt = LoadPreset("UnitPresets/UnitPreset_Grunt");
            var boss = LoadPreset("UnitPresets/UnitPreset_Boss_Despair");
            if (grunt == null)
            {
                Debug.LogError("[Fractured Chorus] Missing UnitPreset_Grunt. Run Create Default Stat Blocks & Presets first.");
                return;
            }

            WriteEncounter(EncounterCatalog.BattleGrunts, new[]
            {
                Spawn(grunt, GridSide.Enemy, 2, 1),
                Spawn(grunt, GridSide.Enemy, 2, 2),
                Spawn(grunt, GridSide.Enemy, 2, 3)
            });

            var elite = LoadPreset("UnitPresets/UnitPreset_Elite_1") ?? grunt;
            WriteEncounter(EncounterCatalog.EliteGrunts, new[]
            {
                Spawn(grunt, GridSide.Enemy, 2, 1),
                SpawnRaw(elite, GridSide.Enemy, 1, 1),
                Spawn(grunt, GridSide.Enemy, 2, 3)
            });

            if (boss == null)
            {
                Debug.LogWarning("[Fractured Chorus] Missing Boss_Despair preset — boss encounter uses grunts only.");
                WriteEncounter(EncounterCatalog.BossDespair, new[]
                {
                    Spawn(grunt, GridSide.Enemy, 2, 1),
                    Spawn(grunt, GridSide.Enemy, 2, 3)
                });
            }
            else
            {
                WriteEncounter(EncounterCatalog.BossDespair, new[]
                {
                    Spawn(grunt, GridSide.Enemy, 2, 1),
                    SpawnRaw(boss, GridSide.Enemy, 1, 1),
                    Spawn(grunt, GridSide.Enemy, 2, 3)
                });
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Fractured Chorus] Encounter SO ready under {Folder}/");
        }

        private static UnitPresetSO LoadPreset(string resourcesPath) =>
            Resources.Load<UnitPresetSO>(resourcesPath);

        private static EncounterUnitSpawn Spawn(UnitPresetSO preset, GridSide side, int displayRow, int displayCol)
        {
            var pos = HoneycombIndex.FromDisplay(side, displayRow, displayCol);
            return new EncounterUnitSpawn
            {
                preset = preset,
                side = side,
                row = pos.Row,
                column = pos.Column
            };
        }

        private static EncounterUnitSpawn SpawnRaw(UnitPresetSO preset, GridSide side, int row, int column) =>
            new EncounterUnitSpawn
            {
                preset = preset,
                side = side,
                row = row,
                column = column
            };

        private static void WriteEncounter(string encounterId, EncounterUnitSpawn[] units)
        {
            var path = $"{Folder}/{encounterId}.asset";
            var encounter = AssetDatabase.LoadAssetAtPath<EncounterDefinitionSO>(path);
            if (encounter == null)
            {
                encounter = ScriptableObject.CreateInstance<EncounterDefinitionSO>();
                AssetDatabase.CreateAsset(encounter, path);
            }

            encounter.encounterId = encounterId;
            encounter.units = units;
            EditorUtility.SetDirty(encounter);
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            var parts = folder.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
#endif
