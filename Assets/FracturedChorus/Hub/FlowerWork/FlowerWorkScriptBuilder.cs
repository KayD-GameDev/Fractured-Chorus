using System.Collections.Generic;
using FracturedChorus.Meta;
using FracturedChorus.Narrative.Vn;
using FracturedChorus.RunMap;
using UnityEngine;

namespace FracturedChorus.Hub.FlowerWork
{
    public static class FlowerWorkScriptBuilder
    {
        public const string ResourcesFolder = "FlowerWork";
        private const string LastScenarioPrefsKey = "fc_flower_last_scenario";

        public const string SpeakerManager = "Shop Manager";
        public const string SpeakerCustomer = "Customer";
        public const string SpeakerRen = "ren";

        public static FlowerWorkScenarioSO PickScenario(IReadOnlyList<FlowerWorkScenarioSO> pool)
        {
            if (pool == null || pool.Count == 0)
            {
                return null;
            }

            if (pool.Count == 1)
            {
                return pool[0];
            }

            var lastId = PlayerPrefs.GetString(LastScenarioPrefsKey, string.Empty);
            var candidates = new List<FlowerWorkScenarioSO>(pool.Count);
            for (var i = 0; i < pool.Count; i++)
            {
                var entry = pool[i];
                if (entry == null)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(lastId) && entry.id == lastId)
                {
                    continue;
                }

                candidates.Add(entry);
            }

            if (candidates.Count == 0)
            {
                candidates.AddRange(pool);
            }

            var pick = candidates[Random.Range(0, candidates.Count)];
            if (pick != null && !string.IsNullOrEmpty(pick.id))
            {
                PlayerPrefs.SetString(LastScenarioPrefsKey, pick.id);
                PlayerPrefs.Save();
            }

            return pick;
        }

        public static FlowerWorkScenarioSO[] LoadPoolFromResources()
        {
            return Resources.LoadAll<FlowerWorkScenarioSO>(ResourcesFolder);
        }

        public static VnScriptSO Build(GameMetaState state, FlowerWorkScenarioSO scenario)
        {
            var script = ScriptableObject.CreateInstance<VnScriptSO>();
            script.id = "flower_shop_work";
            script.nextScene = RunMapSceneCatalog.CampusHub;

            var beats = new List<VnBeat>();
            var playIntro = state == null || !state.HasFlag(StoryFlagIds.FlowerJobIntroDone);

            if (playIntro)
            {
                beats.Add(L(
                    SpeakerManager,
                    "Glad you could make it, Ren. We're busy today — listen carefully to each customer.",
                    VnBgIds.FlowerArrive,
                    setFlags: new[] { StoryFlagIds.FlowerJobIntroDone }));
                beats.Add(L(
                    SpeakerRen,
                    "Understood. I'll do my best.",
                    VnBgIds.FlowerArrive,
                    expression: "neutral"));
            }
            else
            {
                beats.Add(N("Another shift at the flower shop.", VnBgIds.FlowerArrive));
            }

            beats.Add(L(
                SpeakerCustomer,
                scenario != null ? scenario.customerLine : "I'd like flowers for something special…",
                VnBgIds.FlowerCustomer));

            var choiceBeatIndex = beats.Count;
            var choiceLabels = scenario != null && scenario.choices != null && scenario.choices.Length > 0
                ? scenario.choices
                : new[] { "Scarlet roses", "White lilies", "Sunflowers" };
            var correctIndex = scenario != null ? Mathf.Clamp(scenario.correctIndex, 0, choiceLabels.Length - 1) : 0;

            var correctBranchIndex = choiceBeatIndex + 1;
            var wrongBranchIndex = correctBranchIndex + 3;

            var jumps = new int[choiceLabels.Length];
            for (var i = 0; i < jumps.Length; i++)
            {
                jumps[i] = i == correctIndex ? correctBranchIndex : wrongBranchIndex;
            }

            beats.Add(new VnBeat
            {
                kind = VnBeatKind.Choice,
                text = scenario != null ? scenario.thinkPrompt : "Which flowers fit the request?",
                bgId = VnBgIds.FlowerThink,
                choices = choiceLabels,
                choiceNextBeatIndex = jumps,
                showDateHud = true,
                dateHudFromMeta = true
            });

            beats.Add(L(
                SpeakerManager,
                scenario != null ? scenario.correctReply : "Perfect match. The customer looks delighted.",
                VnBgIds.FlowerThink));
            beats.Add(L(
                SpeakerRen,
                "…That worked.",
                VnBgIds.FlowerHappy,
                expression: "smile"));
            beats.Add(End());

            beats.Add(L(
                SpeakerManager,
                scenario != null ? scenario.wrongReply : "Not quite. Remember the request next time.",
                VnBgIds.FlowerThink));
            beats.Add(L(
                SpeakerRen,
                "I'll remember that.",
                VnBgIds.FlowerHappy,
                expression: "neutral"));
            beats.Add(End());

            script.beats = beats.ToArray();
            return script;
        }

        private static VnBeat N(string text, string bgId) => new VnBeat
        {
            kind = VnBeatKind.Narration,
            text = text,
            bgId = bgId,
            showDateHud = true,
            dateHudFromMeta = true
        };

        private static VnBeat L(
            string speakerId,
            string text,
            string bgId,
            string expression = null,
            string[] setFlags = null) => new VnBeat
        {
            kind = VnBeatKind.Line,
            speakerId = speakerId,
            text = text,
            bgId = bgId,
            expression = expression,
            setFlags = setFlags,
            showDateHud = true,
            dateHudFromMeta = true
        };

        private static VnBeat End(params string[] flags) => new VnBeat
        {
            kind = VnBeatKind.End,
            setFlags = flags
        };
    }
}
