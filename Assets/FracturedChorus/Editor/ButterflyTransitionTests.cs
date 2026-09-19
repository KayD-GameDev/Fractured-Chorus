using FracturedChorus.VFX;
using NUnit.Framework;
using UnityEngine;

namespace FracturedChorus.Tests
{
    public class ButterflyTransitionTests
    {
        [Test]
        public void Bezier_AtZero_ReturnsP0()
        {
            var p0 = new Vector2(1.1f, 0.8f);
            var p1 = new Vector2(0.7f, 0.4f);
            var p2 = new Vector2(0.3f, 0.4f);
            var p3 = new Vector2(0.05f, 0.85f);
            var point = ButterflyBezierFlight.EvaluateCubic(p0, p1, p2, p3, 0f);
            Assert.AreEqual(p0.x, point.x, 0.0001f);
            Assert.AreEqual(p0.y, point.y, 0.0001f);
        }

        [Test]
        public void Bezier_AtOne_ReturnsP3()
        {
            var p0 = new Vector2(1.1f, 0.8f);
            var p1 = new Vector2(0.7f, 0.4f);
            var p2 = new Vector2(0.3f, 0.4f);
            var p3 = new Vector2(0.05f, 0.85f);
            var point = ButterflyBezierFlight.EvaluateCubic(p0, p1, p2, p3, 1f);
            Assert.AreEqual(p3.x, point.x, 0.0001f);
            Assert.AreEqual(p3.y, point.y, 0.0001f);
        }

        [Test]
        public void Bezier_Midpoint_StaysInsideControlHull()
        {
            var p0 = new Vector2(0f, 0f);
            var p1 = new Vector2(0f, 1f);
            var p2 = new Vector2(1f, 1f);
            var p3 = new Vector2(1f, 0f);
            var point = ButterflyBezierFlight.EvaluateCubic(p0, p1, p2, p3, 0.5f);
            Assert.Greater(point.x, 0.2f);
            Assert.Less(point.x, 0.8f);
            Assert.Greater(point.y, 0.5f);
        }

        [Test]
        public void Rotation_IsClampedToLimit()
        {
            Assert.AreEqual(20f, ButterflyBezierFlight.ClampVisualRotation(80f, 20f), 0.01f);
            Assert.AreEqual(-20f, ButterflyBezierFlight.ClampVisualRotation(-80f, 20f), 0.01f);
            Assert.AreEqual(12f, ButterflyBezierFlight.ClampVisualRotation(12f, 20f), 0.01f);
        }

        [Test]
        public void Prefab_HasAuthoredHierarchy()
        {
            var yaml = System.IO.File.ReadAllText(
                "Assets/FracturedChorus/VFX/ButterflyTransition/Prefabs/FC_ButterflyTransition.prefab");
            Assert.IsTrue(yaml.Contains("m_Name: FC_ButterflyTransition"));
            Assert.IsTrue(yaml.Contains("m_Name: ButterflyRoot"));
            Assert.IsTrue(yaml.Contains("m_Name: ButterflySprite"));
            Assert.IsTrue(yaml.Contains("m_Name: VFX"));
            Assert.IsTrue(yaml.Contains("m_Name: WaveformTrails"));
            Assert.IsTrue(yaml.Contains("m_Name: FadeOverlay"));
        }

        [Test]
        public void Prefab_WaveChildrenHaveTrailScriptAndSprite()
        {
            var yaml = System.IO.File.ReadAllText(
                "Assets/FracturedChorus/VFX/ButterflyTransition/Prefabs/FC_ButterflyTransition.prefab");
            Assert.IsTrue(yaml.Contains("m_Name: Wave_0"));
            Assert.IsTrue(yaml.Contains("m_Name: Wave_1"));
            Assert.IsTrue(yaml.Contains("m_Name: Wave_2"));
            Assert.IsTrue(yaml.Contains("Assembly-CSharp::FracturedChorus.VFX.ButterflyWaveformTrail"));
            Assert.IsTrue(yaml.Contains("guid: 466692bdeac34e35b888994fea873c60"));
            Assert.IsTrue(yaml.Contains("waveformSprite:"));
        }

        [Test]
        public void DefaultCurves_HaveExpectedEndpoints()
        {
            var scale = ButterflyTransitionController.DefaultScaleCurve();
            Assert.AreEqual(0.7f, scale.Evaluate(0f), 0.02f);
            Assert.AreEqual(1f, scale.Evaluate(1f), 0.02f);
            var emission = ButterflyTransitionController.DefaultEmissionCurve();
            Assert.AreEqual(0.7f, emission.Evaluate(0f), 0.02f);
            Assert.AreEqual(0.45f, emission.Evaluate(1f), 0.02f);
        }

        [Test]
        public void DefaultPath_GoesBottomLeftToTopRight()
        {
            var p0 = ButterflyBezierFlight.DefaultStartPoint;
            var p1 = ButterflyBezierFlight.DefaultControlA;
            var p2 = ButterflyBezierFlight.DefaultControlB;
            var p3 = ButterflyBezierFlight.DefaultEndPoint;
            var mid = ButterflyBezierFlight.EvaluateCubic(p0, p1, p2, p3, 0.5f);
            Assert.Greater(p3.x, p0.x);
            Assert.Greater(p3.y, p0.y);
            Assert.Greater(mid.x, p0.x);
            Assert.Less(mid.x, p3.x);
            Assert.Greater(mid.y, p0.y);
            Assert.Less(mid.y, p3.y);
        }

        [Test]
        public void NormalizedToAnchored_CenterPivot_BottomLeftIsNegative()
        {
            var rect = new Rect(-960f, -540f, 1920f, 1080f);
            var center = new Vector2(0.5f, 0.5f);
            var start = ButterflyBezierFlight.NormalizedToAnchored(
                ButterflyBezierFlight.DefaultStartPoint, rect, center, center);
            var end = ButterflyBezierFlight.NormalizedToAnchored(
                ButterflyBezierFlight.DefaultEndPoint, rect, center, center);
            Assert.Less(start.x, 0f);
            Assert.Less(start.y, 0f);
            Assert.Greater(end.x, 0f);
            Assert.Greater(end.y, 0f);
        }
    }
}
