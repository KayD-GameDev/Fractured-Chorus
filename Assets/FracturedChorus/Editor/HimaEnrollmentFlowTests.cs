using FracturedChorus.Hub;
using FracturedChorus.Meta;
using FracturedChorus.Narrative.Vn;
using FracturedChorus.RunMap;
using NUnit.Framework;
using UnityEngine;

namespace FracturedChorus.Tests
{
    public class HimaEnrollmentFlowTests
    {
        [Test]
        public void Opening_EndsOnTheTrainAndRoutesToCampusHub()
        {
            var script = OpeningInvestigationScriptBuilder.CreateRuntimeInstance();
            try
            {
                Assert.AreEqual(RunMapSceneCatalog.CampusHub, script.nextScene);
                var last = script.beats[script.beats.Length - 1];
                Assert.AreEqual(VnBeatKind.End, last.kind);
                CollectionAssert.Contains(last.setFlags, StoryFlagIds.RenEnRouteHima);
                CollectionAssert.DoesNotContain(last.setFlags, StoryFlagIds.RenArrivedHima);
                CollectionAssert.DoesNotContain(last.setFlags, StoryFlagIds.OpeningCeremony);
                Assert.AreEqual(VnBgIds.LuminaTrainMorning, script.beats[script.beats.Length - 2].bgId);
                Assert.IsFalse(ContainsSpeaker(script.beats, VnSpeakerIds.Charlotte));
            }
            finally
            {
                Object.DestroyImmediate(script);
            }
        }

        [Test]
        public void Enrollment_MeetsCharlotteAndMarksArrival()
        {
            var script = HimaEnrollmentScriptBuilder.CreateRuntimeInstance();
            try
            {
                Assert.AreEqual(RunMapSceneCatalog.CampusHub, script.nextScene);
                Assert.AreEqual(VnBgIds.HimaCampusDay, script.beats[0].bgId);
                Assert.IsTrue(ContainsSpeaker(script.beats, VnSpeakerIds.Charlotte));
                Assert.IsTrue(ContainsSpeaker(script.beats, VnSpeakerIds.Kiki));
                Assert.IsTrue(ContainsSpeaker(script.beats, VnSpeakerIds.Coda));
                Assert.IsTrue(ContainsBg(script.beats, VnBgIds.CgDesyncDetect));
                Assert.IsTrue(ContainsBg(script.beats, VnBgIds.CgResonanceDive));
                Assert.IsTrue(ContainsBg(script.beats, VnBgIds.CadenceFirstLook));
                var last = script.beats[script.beats.Length - 1];
                Assert.AreEqual(CadenceIntroScriptBuilder.LaunchTutorialSignal, last.signalId);
            }
            finally
            {
                Object.DestroyImmediate(script);
            }
        }

        [Test]
        public void Gate_IsActiveOnlyWhileEnrollmentIsPending()
        {
            var enRoute = GameMetaSession.CreateEnRouteToHima();
            Assert.AreEqual(9, enRoute.Calendar.CurrentDate.Month);
            Assert.AreEqual(2, enRoute.Calendar.CurrentDate.Day);
            Assert.AreEqual(DayPhase.Day, enRoute.Calendar.CurrentPhase);
            Assert.IsTrue(enRoute.Calendar.MorningQuizDone);
            Assert.IsFalse(enRoute.HasFlag(StoryFlagIds.RenArrivedHima));
            Assert.IsTrue(HimaEnrollmentGate.IsActive(enRoute));

            enRoute.SetFlag(StoryFlagIds.HimaEnrollmentDone);
            Assert.IsFalse(HimaEnrollmentGate.IsActive(enRoute));
            Assert.IsFalse(HimaEnrollmentGate.IsActive(GameMetaState.CreateHubStart()));
        }

        [Test]
        public void Launch_ConsumesTheEnrollmentScriptOnce()
        {
            HimaEnrollmentLaunch.Cancel();
            Assert.IsFalse(HimaEnrollmentLaunch.TryConsume(out _));

            HimaEnrollmentLaunch.Arm();
            Assert.IsTrue(HimaEnrollmentLaunch.TryConsume(out var script));
            try
            {
                Assert.AreEqual("hima_enrollment_en", script.id);
            }
            finally
            {
                Object.DestroyImmediate(script);
            }

            Assert.IsFalse(HimaEnrollmentLaunch.TryConsume(out _));
        }

        [Test]
        public void Escape_ReturnsToTheHallAfterTheTutorial()
        {
            var script = CadenceIntroScriptBuilder.CreateEscape();
            try
            {
                Assert.AreEqual(VnBgIds.CadenceFirstLook, script.beats[0].bgId);
                Assert.IsTrue(ContainsSpeaker(script.beats, VnSpeakerIds.Ren));
                Assert.IsTrue(ContainsSpeaker(script.beats, VnSpeakerIds.Coda));
                Assert.IsFalse(ContainsSpeaker(script.beats, VnSpeakerIds.Charlotte));
                var last = script.beats[script.beats.Length - 1];
                CollectionAssert.Contains(last.setFlags, StoryFlagIds.HimaEnrollmentDone);
                CollectionAssert.Contains(last.setFlags, StoryFlagIds.CodaMet);
                Assert.AreEqual(RunMapSceneCatalog.CampusHub, script.nextScene);
            }
            finally
            {
                Object.DestroyImmediate(script);
            }
        }

        private static bool ContainsBg(VnBeat[] beats, string bgId)
        {
            for (var i = 0; i < beats.Length; i++)
            {
                if (beats[i].bgId == bgId)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsSpeaker(VnBeat[] beats, string speakerId)
        {
            for (var i = 0; i < beats.Length; i++)
            {
                if (beats[i].speakerId == speakerId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
