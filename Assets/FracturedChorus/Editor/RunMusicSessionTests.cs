using FracturedChorus.Combat.Timeline;
using NUnit.Framework;
using UnityEngine;

namespace FracturedChorus.Tests
{
    public class CombatTimelineProfileTests
    {
        [Test]
        public void ApplyRun_Sets689BeatsAndZeroIntro()
        {
            CombatTimelineProfile.ApplyRun();
            Assert.AreEqual(689, CombatTimelineProfile.TotalBeats);
            Assert.AreEqual(0, CombatTimelineProfile.CombatIntroBeatCount);
            Assert.AreEqual(0f, CombatTimelineProfile.CombatIntroDurationSec);
        }

        [Test]
        public void ApplyBoss_Restores677BeatsAndIntro()
        {
            CombatTimelineProfile.ApplyRun();
            CombatTimelineProfile.ApplyBoss();
            Assert.AreEqual(677, CombatTimelineProfile.TotalBeats);
            Assert.AreEqual(12, CombatTimelineProfile.CombatIntroBeatCount);
            Assert.Greater(CombatTimelineProfile.CombatIntroDurationSec, 5f);
        }

        [Test]
        public void MusicalBeatToTime_ZeroOffset_BeatZeroAtZero()
        {
            var map = ScriptableObject.CreateInstance<FracturedChorus.Audio.MusicBeatMapSO>();
            map.EditorSetData(null, 152f, 0f);
            Assert.AreEqual(0f, map.MusicalBeatToTime(0f), 0.0001f);
            Assert.AreEqual(0.394737f, map.BeatSpanSec, 0.001f);
            Object.DestroyImmediate(map);
        }
    }
}
