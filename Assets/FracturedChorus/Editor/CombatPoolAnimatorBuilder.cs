#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using FracturedChorus.Combat.Bootstrap;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace FracturedChorus.Editor
{
    public static class CombatPoolAnimatorBuilder
    {
        private const string OutputFolder = "Assets/FracturedChorus/Resources/CombatPoolAnimators";
        private const float FrameDurationSec = 0.1f;

        [MenuItem("Fractured Chorus/Combat/Build Pool Enemy & Elite Animators")]
        public static void BuildAll()
        {
            EnsureFolder(OutputFolder);

            BuildForKey(CombatEnemyKeys.Enemy1, "Enemy 1");
            BuildForKey(CombatEnemyKeys.Enemy2, "Enemy 2");
            BuildForKey(CombatEnemyKeys.Enemy3, "Enemy 3");
            BuildForKey(CombatEnemyKeys.Elite1, "Elite 1");
            BuildForKey(CombatEnemyKeys.Elite2, "Elite 2");
            BuildForKey(CombatEnemyKeys.Elite3, "Elite 3");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Fractured Chorus] Pool combat animators rebuilt from Art/Characters Enemy & Elite clips.");
        }

        [InitializeOnLoadMethod]
        private static void AutoFillEmptyClips()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    return;
                }

                try
                {
                    RestoreCenterAlignmentAll();

                    var probe = AssetDatabase.LoadAssetAtPath<AnimationClip>(
                        "Assets/FracturedChorus/Art/Characters/Enemy 3/Enemy 3 - Idle.anim")
                                ?? AssetDatabase.LoadAssetAtPath<AnimationClip>(
                                    "Assets/FracturedChorus/Art/Characters/Enemy 1/Enemy 1 - Idle.anim");
                    if (probe == null)
                    {
                        return;
                    }

                    var keys = AnimationUtility.GetObjectReferenceCurve(
                        probe,
                        EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite"));
                    if (keys != null && keys.Length > 0)
                    {
                        return;
                    }

                    BuildAll();
                }
                catch (System.Exception error)
                {
                    Debug.LogError($"[Fractured Chorus] Pool animator auto-build failed: {error.Message}");
                }
            };
        }

        private static void BuildForKey(string unitKey, string artFolderName)
        {
            var artFolder = $"Assets/FracturedChorus/Art/Characters/{artFolderName}";
            if (!AssetDatabase.IsValidFolder(artFolder))
            {
                Debug.LogWarning($"[Fractured Chorus] Missing art folder: {artFolder}");
                return;
            }

            NormalizeSpritesInFolder(artFolder);

            var clipGuids = AssetDatabase.FindAssets("t:AnimationClip", new[] { artFolder });
            if (clipGuids.Length == 0)
            {
                Debug.LogWarning($"[Fractured Chorus] No animation clips in {artFolder}");
                return;
            }

            var clips = new List<AnimationClip>();
            foreach (var guid in clipGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip == null)
                {
                    continue;
                }

                FillClipFromFolderSprites(clip, artFolder);
                clips.Add(clip);
            }

            clips = clips.OrderBy(c => c.name).ToList();
            var controllerPath = $"{OutputFolder}/Unit_{unitKey}.controller";
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(controllerPath);
            }

            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var root = controller.layers[0].stateMachine;
            AnimatorState defaultState = null;

            foreach (var clip in clips)
            {
                var state = root.AddState(clip.name);
                state.motion = clip;
                state.writeDefaultValues = false;
                if (defaultState == null && clip.name.IndexOf("Idle", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    defaultState = state;
                }
            }

            root.defaultState = defaultState ?? (root.states.Length > 0 ? root.states[0].state : null);
            EditorUtility.SetDirty(controller);
        }

        private static void RestoreCenterAlignmentAll()
        {
            RestoreCenterAlignment("Assets/FracturedChorus/Art/Characters/Enemy 1");
            RestoreCenterAlignment("Assets/FracturedChorus/Art/Characters/Enemy 2");
            RestoreCenterAlignment("Assets/FracturedChorus/Art/Characters/Enemy 3");
            RestoreCenterAlignment("Assets/FracturedChorus/Art/Characters/Elite 1");
            RestoreCenterAlignment("Assets/FracturedChorus/Art/Characters/Elite 2");
            RestoreCenterAlignment("Assets/FracturedChorus/Art/Characters/Elite 3");
        }

        private static void RestoreCenterAlignment(string artFolder)
        {
            if (!AssetDatabase.IsValidFolder(artFolder))
            {
                return;
            }

            NormalizeSpritesInFolder(artFolder);
        }

        private static void NormalizeSpritesInFolder(string artFolder)
        {
            var textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { artFolder });
            var factories = new SpriteDataProviderFactories();
            factories.Init();
            foreach (var guid in textureGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null || importer.textureType != TextureImporterType.Sprite)
                {
                    continue;
                }

                var dirty = false;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                if (settings.spriteAlignment != (int)SpriteAlignment.Center
                    || settings.spritePivot != new Vector2(0.5f, 0.5f))
                {
                    settings.spriteAlignment = (int)SpriteAlignment.Center;
                    settings.spritePivot = new Vector2(0.5f, 0.5f);
                    importer.SetTextureSettings(settings);
                    dirty = true;
                }

                var dataProvider = factories.GetSpriteEditorDataProviderFromObject(importer);
                if (dataProvider != null)
                {
                    dataProvider.InitSpriteEditorDataProvider();
                    var rects = dataProvider.GetSpriteRects();
                    if (rects != null)
                    {
                        var sheetDirty = false;
                        for (var i = 0; i < rects.Length; i++)
                        {
                            var slice = rects[i];
                            if (slice.alignment == SpriteAlignment.Center
                                && slice.pivot == new Vector2(0.5f, 0.5f))
                            {
                                continue;
                            }

                            slice.alignment = SpriteAlignment.Center;
                            slice.pivot = new Vector2(0.5f, 0.5f);
                            rects[i] = slice;
                            sheetDirty = true;
                        }

                        if (sheetDirty)
                        {
                            dataProvider.SetSpriteRects(rects);
                            dataProvider.Apply();
                            dirty = true;
                        }
                    }
                }

                if (dirty)
                {
                    importer.SaveAndReimport();
                }
            }
        }

        private static void FillClipFromFolderSprites(AnimationClip clip, string artFolder)
        {
            var sprites = FindSpritesForClip(clip.name, artFolder);
            if (sprites.Count == 0)
            {
                Debug.LogWarning($"[Fractured Chorus] No sprite frames for clip '{clip.name}' in {artFolder}");
                return;
            }

            var binding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
            var keys = new ObjectReferenceKeyframe[sprites.Count];
            for (var i = 0; i < sprites.Count; i++)
            {
                keys[i] = new ObjectReferenceKeyframe
                {
                    time = i * FrameDurationSec,
                    value = sprites[i]
                };
            }

            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            var loop = clip.name.IndexOf("Idle", System.StringComparison.OrdinalIgnoreCase) >= 0
                       || clip.name.IndexOf("Moving", System.StringComparison.OrdinalIgnoreCase) >= 0;
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            clip.frameRate = 1f / FrameDurationSec;
            EditorUtility.SetDirty(clip);
        }

        private static List<Sprite> FindSpritesForClip(string clipName, string artFolder)
        {
            var textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { artFolder });
            var ranked = new List<(int rank, string path)>();
            foreach (var guid in textureGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var rank = ScoreTextureForClip(clipName, path);
                if (rank > 0)
                {
                    ranked.Add((rank, path));
                }
            }

            if (ranked.Count == 0)
            {
                return new List<Sprite>();
            }

            var bestPath = ranked.OrderByDescending(e => e.rank).ThenBy(e => e.path).First().path;
            var sprites = AssetDatabase.LoadAllAssetsAtPath(bestPath)
                .OfType<Sprite>()
                .OrderBy(s => s.name)
                .ToList();
            if (sprites.Count == 0)
            {
                var single = AssetDatabase.LoadAssetAtPath<Sprite>(bestPath);
                if (single != null)
                {
                    sprites.Add(single);
                }
            }

            return sprites;
        }

        private static int ScoreTextureForClip(string clipName, string texturePath)
        {
            var file = System.IO.Path.GetFileNameWithoutExtension(texturePath);
            if (file.IndexOf("Sprite", System.StringComparison.OrdinalIgnoreCase) < 0
                && file.IndexOf("Idle", System.StringComparison.OrdinalIgnoreCase) < 0)
            {
                if (clipName.IndexOf("Idle", System.StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return 0;
                }
            }

            if (Contains(clipName, "Skill 2") || Contains(clipName, "Skill2"))
            {
                if (Contains(file, "Skill 2") || Contains(file, "Coil") || Contains(file, "Broken")
                    || Contains(file, "Evade"))
                {
                    return 30;
                }

                return 0;
            }

            if (Contains(clipName, "Skill 1") || (Contains(clipName, "Skill") && !Contains(clipName, "Skill 2")))
            {
                if (Contains(file, "Guard"))
                {
                    return 25;
                }

                if (Contains(file, "Skill") || Contains(file, "Coil"))
                {
                    return 30;
                }

                if (Contains(file, "Evade"))
                {
                    return 5;
                }

                return 0;
            }

            if (Contains(clipName, "Death") || Contains(clipName, "Dead"))
            {
                if (Contains(file, "Dead") || Contains(file, "Death") || Contains(file, "Broken"))
                {
                    return 30;
                }

                return 0;
            }

            var keywords = new[] { "Guard", "Hurt", "Moving", "Evade", "Idle" };
            foreach (var keyword in keywords)
            {
                if (!Contains(clipName, keyword))
                {
                    continue;
                }

                return Contains(file, keyword) ? 30 : 0;
            }

            return 0;
        }

        private static bool Contains(string value, string token) =>
            value.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0;

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
