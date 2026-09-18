using FracturedChorus.Combat.Presentation;
using NUnit.Framework;

namespace FracturedChorus.Tests
{
    public class AstraStageTvSequenceTests
    {
        [Test]
        public void Tick_DropThenRollThenLock_WithSeed()
        {
            var seq = new AstraStageTvSequence();
            seq.Begin(1f, 2.2f, 6f, 0.45f, seed: 17);
            Assert.AreEqual(AstraStageTvPhase.Dropping, seq.Phase);

            seq.Tick(0.5f);
            Assert.AreEqual(AstraStageTvPhase.Dropping, seq.Phase);
            Assert.Greater(seq.DropT, 0.4f);
            Assert.Less(seq.DropT, 1f);

            seq.Tick(0.6f);
            Assert.AreEqual(AstraStageTvPhase.Rolling, seq.Phase);
            Assert.AreEqual(1f, seq.DropT);

            seq.Tick(1.0f);
            Assert.AreEqual(AstraStageTvPhase.Rolling, seq.Phase);
            Assert.Greater(seq.ReelOffset, 0f);

            seq.Tick(2.0f);
            Assert.AreEqual(AstraStageTvPhase.Locked, seq.Phase);
            Assert.GreaterOrEqual(seq.LockedFaceIndex, 0);
            Assert.Less(seq.LockedFaceIndex, AstraStageTvSequence.FaceCount);
        }

        [Test]
        public void Tick_DoesNotRollUntilDropCompletes()
        {
            var seq = new AstraStageTvSequence();
            seq.Begin(1f, 2f, 6f, 0.4f, seed: 1);
            seq.Tick(0.99f);
            Assert.AreEqual(AstraStageTvPhase.Dropping, seq.Phase);
            Assert.AreEqual(0f, seq.ReelOffset);
        }

        [Test]
        public void SameSeed_LocksSameFace()
        {
            var a = new AstraStageTvSequence();
            var b = new AstraStageTvSequence();
            a.Begin(0.2f, 0.8f, 8f, 0.2f, seed: 3);
            b.Begin(0.2f, 0.8f, 8f, 0.2f, seed: 3);
            for (var i = 0; i < 40; i++)
            {
                a.Tick(0.05f);
                b.Tick(0.05f);
            }

            Assert.AreEqual(AstraStageTvPhase.Locked, a.Phase);
            Assert.AreEqual(a.LockedFaceIndex, b.LockedFaceIndex);
        }

        [Test]
        public void BeginReelOnly_SkipsDrop()
        {
            var seq = new AstraStageTvSequence();
            seq.BeginReelOnly(0.8f, 8f, 0.2f, seed: 5);
            Assert.AreEqual(AstraStageTvPhase.Rolling, seq.Phase);
            Assert.AreEqual(1f, seq.DropT);

            seq.Tick(0.05f);
            Assert.AreEqual(AstraStageTvPhase.Rolling, seq.Phase);
            Assert.Greater(seq.ReelOffset, 0f);
        }

        [Test]
        public void BeginReelOnly_ExcludesPreviousFace()
        {
            var baseline = new AstraStageTvSequence();
            baseline.Begin(0.05f, 0.8f, 8f, 0.2f, seed: 3);
            for (var i = 0; i < 40; i++)
            {
                baseline.Tick(0.05f);
            }

            Assert.AreEqual(AstraStageTvPhase.Locked, baseline.Phase);
            var excluded = baseline.LockedFaceIndex;

            var seq = new AstraStageTvSequence();
            seq.BeginReelOnly(0.8f, 8f, 0.2f, seed: 3, excludeFaceIndex: excluded);
            for (var i = 0; i < 40; i++)
            {
                seq.Tick(0.05f);
            }

            Assert.AreEqual(AstraStageTvPhase.Locked, seq.Phase);
            Assert.AreNotEqual(excluded, seq.LockedFaceIndex);
        }
    }
}
