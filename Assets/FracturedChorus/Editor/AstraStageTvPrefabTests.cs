using FracturedChorus.Combat.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Tests
{
    public class AstraStageTvPrefabTests
    {
        private const string PrefabPath =
            "Assets/FracturedChorus/Resources/UI/Combat/Boss/Astra/AstraStageTv.prefab";
        private const string ConfigPath =
            "Assets/FracturedChorus/Resources/UI/Combat/Boss/Astra/AstraStageTvConfig.asset";
        private const string SpriteRoot =
            "Assets/FracturedChorus/Resources/UI/Combat/Boss/Astra/StageTv/";

        [Test]
        public void Config_HasDistinctFrameAndFiveFaces()
        {
            var config = AssetDatabase.LoadAssetAtPath<AstraStageTvConfig>(ConfigPath);
            Assert.IsNotNull(config);
            Assert.IsNotNull(config.FrameSprite);
            Assert.IsNotNull(config.FaceSprites);
            Assert.AreEqual(AstraStageTvSequence.FaceCount, config.FaceSprites.Length);

            // Fallback rest pose only — the scene rect overrides it once authored.
            Assert.That(config.RestNormalizedPos.x, Is.InRange(0f, 1f));
            Assert.That(config.RestNormalizedPos.y, Is.InRange(0f, 1f));
            Assert.Greater(config.RestSizePx.x, 32f);
            Assert.Greater(config.RestSizePx.y, 32f);
            Assert.AreEqual(2, config.MoodDurationPhases);
            Assert.Greater(config.HateCoverCostMult, 1f);
            Assert.Less(config.AngerQteWindowMult, 1f);

            var paths = new[]
            {
                AssetDatabase.GetAssetPath(config.FrameSprite),
                AssetDatabase.GetAssetPath(config.FaceSprites[0]),
                AssetDatabase.GetAssetPath(config.FaceSprites[1]),
                AssetDatabase.GetAssetPath(config.FaceSprites[2]),
                AssetDatabase.GetAssetPath(config.FaceSprites[3]),
                AssetDatabase.GetAssetPath(config.FaceSprites[4])
            };

            for (var i = 0; i < paths.Length; i++)
            {
                Assert.IsFalse(string.IsNullOrEmpty(paths[i]), "sprite " + i);
                for (var j = i + 1; j < paths.Length; j++)
                {
                    Assert.AreNotEqual(paths[i], paths[j], paths[i] + " merged with " + paths[j]);
                }
            }
        }

        [Test]
        public void SeparatePngs_ExistOnDisk()
        {
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Sprite>(SpriteRoot + "astra_stage_tv_frame_v1.png"));
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Sprite>(SpriteRoot + "astra_tv_face_joy_v1.png"));
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Sprite>(SpriteRoot + "astra_tv_face_anger_v1.png"));
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Sprite>(SpriteRoot + "astra_tv_face_love_v1.png"));
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Sprite>(SpriteRoot + "astra_tv_face_hate_v1.png"));
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Sprite>(SpriteRoot + "astra_tv_face_sorrow_v1.png"));
        }

        [Test]
        public void Prefab_HasFrameScreenMaskAndFaceReel()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(prefab);
            Assert.IsNotNull(prefab.GetComponent<AstraStageTvView>());
            var frame = Find(prefab.transform, AstraStageTvView.FrameChildName);
            var screen = Find(prefab.transform, AstraStageTvView.ScreenChildName);
            var reel = Find(prefab.transform, AstraStageTvView.FaceReelChildName);
            Assert.IsNotNull(frame);
            Assert.IsNotNull(frame.GetComponent<Image>());
            Assert.IsNotNull(screen);
            Assert.IsNotNull(screen.GetComponent<RectMask2D>());
            Assert.IsNotNull(reel);
        }

        [Test]
        public void EnsureBuilt_CreatesExactlyFiveFaceImages()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(prefab);
            var instance = Object.Instantiate(prefab);
            try
            {
                var view = instance.GetComponent<AstraStageTvView>();
                Assert.IsNotNull(view);
                view.EnsureBuilt();
                var reel = Find(instance.transform, AstraStageTvView.FaceReelChildName);
                Assert.IsNotNull(reel);
                var images = reel.GetComponentsInChildren<Image>(true);
                Assert.AreEqual(AstraStageTvSequence.FaceCount, images.Length);
            }
            finally
            {
                Object.DestroyImmediate(instance);
            }
        }

        [Test]
        public void SceneRect_DrivesRestPose_AndDropReturnsToIt()
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas));
            try
            {
                var canvasRt = canvasGo.GetComponent<RectTransform>();
                canvasRt.sizeDelta = new Vector2(1920f, 1080f);

                var tvGo = new GameObject(AstraStageTvView.ObjectName, typeof(RectTransform));
                var rect = tvGo.GetComponent<RectTransform>();
                rect.SetParent(canvasRt, false);
                rect.anchorMin = new Vector2(0.42f, 0.61f);
                rect.anchorMax = new Vector2(0.42f, 0.61f);
                rect.sizeDelta = new Vector2(333f, 333f);
                rect.anchoredPosition = new Vector2(7f, 9f);

                var view = tvGo.AddComponent<AstraStageTvView>();
                view.EnsureBuilt();

                Assert.AreEqual(new Vector2(0.42f, 0.61f), rect.anchorMin, "anchors overwritten");
                Assert.AreEqual(new Vector2(333f, 333f), rect.sizeDelta, "size overwritten");
                Assert.AreEqual(new Vector2(7f, 9f), rect.anchoredPosition, "rest position overwritten");

                view.ParkAboveForIntro();
                Assert.Greater(rect.anchoredPosition.y, 9f, "park must sit above the authored rest");

                view.ShowAtRest();
                Assert.AreEqual(new Vector2(7f, 9f), rect.anchoredPosition, "drop target must be the scene rest");
            }
            finally
            {
                Object.DestroyImmediate(canvasGo);
            }
        }

        [Test]
        public void SceneScreenColor_SurvivesEnsureBuilt()
        {
            var canvasGo = new GameObject("Canvas", typeof(Canvas));
            try
            {
                var tvGo = new GameObject(AstraStageTvView.ObjectName, typeof(RectTransform));
                tvGo.GetComponent<RectTransform>().SetParent(canvasGo.transform, false);

                var screenGo = new GameObject(AstraStageTvView.ScreenChildName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                screenGo.transform.SetParent(tvGo.transform, false);
                var authored = new Color(0.12f, 0.34f, 0.56f, 0.8f);
                screenGo.GetComponent<Image>().color = authored;

                var view = tvGo.AddComponent<AstraStageTvView>();
                view.EnsureBuilt();

                var screen = Find(tvGo.transform, AstraStageTvView.ScreenChildName);
                Assert.IsNotNull(screen);
                var image = screen.GetComponent<Image>();
                Assert.IsNotNull(image);
                Assert.AreEqual(authored, image.color, "Screen Image color/alpha must stay authored");
            }
            finally
            {
                Object.DestroyImmediate(canvasGo);
            }
        }

        private static Transform Find(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name)
                {
                    return t;
                }
            }

            return null;
        }
    }
}
