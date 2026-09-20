using FracturedChorus.UI;
using NUnit.Framework;

namespace FracturedChorus.Tests
{
    public class PartyCardRowFitTests
    {
        [Test]
        public void FourCards_FitAtDefaultScale()
        {
            var scale = PartyCardLayout.ComputeRowFitScale(
                4,
                PartyCardLayout.CardWidth,
                PartyCardLayout.CardGap,
                PartyCardLayout.DefaultStatusBarWidth);
            Assert.AreEqual(1f, scale);
            Assert.LessOrEqual(
                4f * PartyCardLayout.CardWidth + 3f * PartyCardLayout.CardGap,
                PartyCardLayout.DefaultStatusBarWidth);
        }

        [Test]
        public void FiveAndSixCards_ScaleDownToStayOnOneRow()
        {
            var scale5 = PartyCardLayout.ComputeRowFitScale(
                5,
                PartyCardLayout.CardWidth,
                PartyCardLayout.CardGap,
                PartyCardLayout.DefaultStatusBarWidth);
            var scale6 = PartyCardLayout.ComputeRowFitScale(
                6,
                PartyCardLayout.CardWidth,
                PartyCardLayout.CardGap,
                PartyCardLayout.DefaultStatusBarWidth);

            Assert.Less(scale5, 1f);
            Assert.Less(scale6, 1f);
            Assert.Less(scale6, scale5);

            const float epsilon = 0.02f;
            Assert.LessOrEqual(
                5f * PartyCardLayout.CardWidth * scale5 + 4f * PartyCardLayout.CardGap,
                PartyCardLayout.DefaultStatusBarWidth + epsilon);
            Assert.LessOrEqual(
                6f * PartyCardLayout.CardWidth * scale6 + 5f * PartyCardLayout.CardGap,
                PartyCardLayout.DefaultStatusBarWidth + epsilon);
        }
    }
}
