using FracturedChorus.Hub.FlowerWork;
using FracturedChorus.Meta;
using FracturedChorus.Narrative.Vn;
using NUnit.Framework;
using UnityEngine;

namespace FracturedChorus.Tests
{
    public class FlowerWorkScriptBuilderTests
    {
        [Test]
        public void Build_OpensWithMorningGreetingAndVoiceCue()
        {
            VnScriptSO script = null;
            try
            {
                script = FlowerWorkScriptBuilder.Build(GameMetaState.CreateHubStart(), null);
                Assert.IsNotNull(script.beats);
                Assert.GreaterOrEqual(script.beats.Length, 3);
                Assert.AreEqual(VnBeatKind.Line, script.beats[0].kind);
                Assert.AreEqual(VnAudioIds.FlowerShopGreet, script.beats[0].sfxId);
                Assert.AreEqual(VnSpeakerIds.FlowerOwner, script.beats[0].speakerId);
                Assert.AreEqual(VnBeatKind.Fade, script.beats[2].kind);
                Assert.AreEqual(FlowerWorkScriptBuilder.TimePassFadeSeconds, script.beats[2].duration, 0.01f);
            }
            finally
            {
                if (script != null)
                {
                    Object.DestroyImmediate(script);
                }
            }
        }

        [Test]
        public void Build_UsesSingleFlowerShopBackground()
        {
            var script = FlowerWorkScriptBuilder.Build(GameMetaState.CreateNew(), null);
            try
            {
                Assert.IsNotNull(script);
                Assert.IsNotNull(script.beats);

                for (var i = 0; i < script.beats.Length; i++)
                {
                    var bgId = script.beats[i].bgId;
                    if (string.IsNullOrEmpty(bgId))
                    {
                        continue;
                    }

                    Assert.AreEqual(VnBgIds.FlowerShop, bgId);
                }
            }
            finally
            {
                if (script != null)
                {
                    Object.DestroyImmediate(script);
                }
            }
        }

        [Test]
        public void Build_CorrectChoiceJumpsToSuccessBranch()
        {
            var scenario = ScriptableObject.CreateInstance<FlowerWorkScenarioSO>();
            scenario.id = "test";
            scenario.choices = new[] { "A", "B", "C" };
            scenario.correctIndex = 1;
            scenario.customerLine = "Need flowers.";
            scenario.thinkPrompt = "Pick one.";
            scenario.correctReply = "Yes.";
            scenario.wrongReply = "No.";

            VnScriptSO script = null;
            try
            {
            script = FlowerWorkScriptBuilder.Build(GameMetaState.CreateHubStart(), scenario);
            VnBeat choice = null;
            var choiceIndex = -1;
            for (var i = 0; i < script.beats.Length; i++)
            {
                if (script.beats[i].kind != VnBeatKind.Choice)
                {
                    continue;
                }

                choice = script.beats[i];
                choiceIndex = i;
                break;
            }

            Assert.IsNotNull(choice);
            Assert.AreEqual(3, choice.choices.Length);
            Assert.AreEqual(choiceIndex + 1, choice.choiceNextBeatIndex[1]);
            Assert.AreEqual(choiceIndex + 5, choice.choiceNextBeatIndex[0]);
            Assert.AreEqual(choiceIndex + 5, choice.choiceNextBeatIndex[2]);
            }
            finally
            {
                if (script != null)
                {
                    Object.DestroyImmediate(script);
                }

                Object.DestroyImmediate(scenario);
            }
        }
    }
}
