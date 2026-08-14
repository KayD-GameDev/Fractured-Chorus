using FracturedChorus.UI.Loading;
using NUnit.Framework;

namespace FracturedChorus.Tests
{
    public class LoadingProgressTests
    {
        [Test]
        public void MapAsyncProgress_Zero_IsZero()
        {
            Assert.AreEqual(0f, LoadingProgress.MapAsyncProgress(0f), 0.0001f);
        }

        [Test]
        public void MapAsyncProgress_Cap_IsOne()
        {
            Assert.AreEqual(1f, LoadingProgress.MapAsyncProgress(0.9f), 0.0001f);
        }

        [Test]
        public void MapAsyncProgress_HalfCap_IsHalf()
        {
            Assert.AreEqual(0.5f, LoadingProgress.MapAsyncProgress(0.45f), 0.0001f);
        }

        [Test]
        public void MapAsyncProgress_AboveCap_ClampsToOne()
        {
            Assert.AreEqual(1f, LoadingProgress.MapAsyncProgress(1f), 0.0001f);
        }

        [Test]
        public void CanActivate_RequiresFillAndHold()
        {
            Assert.IsFalse(LoadingProgress.CanActivate(1f, 0.79f));
            Assert.IsFalse(LoadingProgress.CanActivate(0.98f, 1f));
            Assert.IsTrue(LoadingProgress.CanActivate(0.99f, 0.80f));
        }
    }
}
