using System.Collections.Generic;
using FracturedChorus.Meta;
using FracturedChorus.Narrative;
using FracturedChorus.Narrative.Vn;
using FracturedChorus.RunMap;
using UnityEngine;

namespace FracturedChorus.Hub.FlowerWork
{
    public static class FlowerWorkScriptBuilder
    {
        public const string ResourcesFolder = "FlowerWork";
        private const string LastScenarioPrefsKey = "fc_flower_last_scenario";
        public const float EntryDelaySeconds = 1f;
        public const float TimePassFadeSeconds = 3.5f;

        public const string SpeakerRen = VnSpeakerIds.Ren;

        private const string DefaultManagerReplyToCustomer =
            "Of course — let me see what we can do for you.";

        private const string DefaultManagerAskRen = "Ren, what would you recommend?";

        private const string DefaultCustomerThanks = "Thank you so much — they're perfect!";

        private const string DefaultCustomerUnhappy = "...I see. I was hoping for something different.";

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
            var customerSpeakerId = FlowerWorkCustomerSpeakers.ResolveSpeakerId(scenario);
            var playerName = RunProfile.PlayerName;
            if (string.IsNullOrWhiteSpace(playerName))
            {
                playerName = RunProfile.DefaultNameSuggestion;
            }

            beats.Add(L(
                VnSpeakerIds.FlowerOwner,
                $"Good morning, {playerName}. Let's do our best together today.",
                VnBgIds.FlowerShop,
                sfxId: VnAudioIds.FlowerShopGreet,
                setFlags: state == null || !state.HasFlag(StoryFlagIds.FlowerJobIntroDone)
                    ? new[] { StoryFlagIds.FlowerJobIntroDone }
                    : null));
            beats.Add(L(
                SpeakerRen,
                "Morning. I'll give it everything I've got.",
                VnBgIds.FlowerShop,
                expression: "neutral"));
            beats.Add(Fade(TimePassFadeSeconds));

            beats.Add(L(
                customerSpeakerId,
                scenario != null ? scenario.customerLine : "I'd like flowers for something special…",
                VnBgIds.FlowerShop));
            beats.Add(L(
                VnSpeakerIds.FlowerOwner,
                PickScenarioLine(scenario?.managerReplyToCustomer, DefaultManagerReplyToCustomer),
                VnBgIds.FlowerShop));
            beats.Add(L(
                VnSpeakerIds.FlowerOwner,
                PickScenarioLine(scenario?.managerAskRen, DefaultManagerAskRen),
                VnBgIds.FlowerShop));

            var choiceBeatIndex = beats.Count;
            var choiceLabels = scenario != null && scenario.choices != null && scenario.choices.Length > 0
                ? scenario.choices
                : new[] { "Scarlet roses", "White lilies", "Sunflowers" };
            var correctIndex = scenario != null ? Mathf.Clamp(scenario.correctIndex, 0, choiceLabels.Length - 1) : 0;

            var correctBranchIndex = choiceBeatIndex + 1;
            var wrongBranchIndex = correctBranchIndex + 5;

            var jumps = new int[choiceLabels.Length];
            for (var i = 0; i < jumps.Length; i++)
            {
                jumps[i] = i == correctIndex ? correctBranchIndex : wrongBranchIndex;
            }

            beats.Add(new VnBeat
            {
                kind = VnBeatKind.Choice,
                text = scenario != null ? scenario.thinkPrompt : "Which flowers fit the request?",
                bgId = VnBgIds.FlowerShop,
                choices = choiceLabels,
                choiceNextBeatIndex = jumps,
                showDateHud = true,
                dateHudFromMeta = true
            });

            beats.Add(L(
                customerSpeakerId,
                PickScenarioLine(scenario?.customerThanks, DefaultCustomerThanks),
                VnBgIds.FlowerShop));
            beats.Add(L(
                VnSpeakerIds.FlowerOwner,
                scenario != null ? scenario.correctReply : "Perfect match. The customer looks delighted.",
                VnBgIds.FlowerShop));
            beats.Add(Signal(FlowerWorkSignals.GrantResonance));
            beats.Add(L(
                SpeakerRen,
                "…That worked.",
                VnBgIds.FlowerShop,
                expression: "smile"));
            beats.Add(End());

            beats.Add(L(
                customerSpeakerId,
                PickScenarioLine(scenario?.customerUnhappy, DefaultCustomerUnhappy),
                VnBgIds.FlowerShop));
            beats.Add(L(
                VnSpeakerIds.FlowerOwner,
                scenario != null ? scenario.wrongReply : "Not quite. Remember the request next time.",
                VnBgIds.FlowerShop));
            beats.Add(L(
                SpeakerRen,
                "I'll remember that.",
                VnBgIds.FlowerShop,
                expression: "neutral"));
            beats.Add(End());

            script.beats = beats.ToArray();
            return script;
        }

        private static string PickScenarioLine(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        private static VnBeat Fade(float duration) => new VnBeat
        {
            kind = VnBeatKind.Fade,
            duration = duration,
            bgId = VnBgIds.FlowerShop,
            showDateHud = true,
            dateHudFromMeta = true
        };

        private static VnBeat Signal(string signalId) => new VnBeat
        {
            kind = VnBeatKind.Cue,
            bgId = VnBgIds.FlowerShop,
            signalId = signalId,
            showDateHud = true,
            dateHudFromMeta = true
        };

        private static VnBeat L(
            string speakerId,
            string text,
            string bgId,
            string expression = null,
            string sfxId = null,
            string[] setFlags = null) => new VnBeat
        {
            kind = VnBeatKind.Line,
            speakerId = speakerId,
            text = text,
            bgId = bgId,
            expression = expression,
            sfxId = sfxId,
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
