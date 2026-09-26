using System.Collections.Generic;

namespace FracturedChorus.Narrative.Vn
{
    public static class VnRuntimeSpeakers
    {
        private static readonly Dictionary<string, VnSpeakerDefinitionSO> Speakers =
            new Dictionary<string, VnSpeakerDefinitionSO>();

        public static void Clear()
        {
            Speakers.Clear();
        }

        public static void Set(VnSpeakerDefinitionSO speaker)
        {
            if (speaker == null || string.IsNullOrWhiteSpace(speaker.speakerId))
            {
                return;
            }

            Speakers[speaker.speakerId] = speaker;
        }

        public static bool TryGet(string speakerId, out VnSpeakerDefinitionSO speaker)
        {
            if (string.IsNullOrWhiteSpace(speakerId))
            {
                speaker = null;
                return false;
            }

            return Speakers.TryGetValue(speakerId, out speaker) && speaker != null;
        }
    }
}
