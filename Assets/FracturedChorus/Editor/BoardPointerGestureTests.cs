using FracturedChorus.UI;
using NUnit.Framework;
using UnityEngine;

namespace FracturedChorus.Tests
{
    public class BoardPointerGestureTests
    {
        [Test]
        public void ShouldCommitDrag_False_WhenDistanceAtOrBelowThreshold()
        {
            var down = new Vector2(100f, 100f);
            Assert.IsFalse(BoardPointerGesture.ShouldCommitDrag(down, down + new Vector2(8f, 0f), 8f));
            Assert.IsFalse(BoardPointerGesture.ShouldCommitDrag(down, down, 8f));
        }

        [Test]
        public void ShouldCommitDrag_True_WhenDistanceAboveThreshold()
        {
            var down = new Vector2(100f, 100f);
            Assert.IsTrue(BoardPointerGesture.ShouldCommitDrag(down, down + new Vector2(8.1f, 0f), 8f));
        }

        [Test]
        public void IsClick_True_OnlyWhenNotCommitted()
        {
            var down = new Vector2(50f, 50f);
            Assert.IsTrue(BoardPointerGesture.IsClick(down, down + new Vector2(3f, 4f), 8f));
            Assert.IsFalse(BoardPointerGesture.IsClick(down, down + new Vector2(10f, 0f), 8f));
        }
    }
}
