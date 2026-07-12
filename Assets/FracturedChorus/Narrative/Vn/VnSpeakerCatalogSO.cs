using System.Collections.Generic;
using UnityEngine;

namespace FracturedChorus.Narrative.Vn
{
    [CreateAssetMenu(
        fileName = "VnSpeakerCatalog",
        menuName = "Fractured Chorus/Narrative/VN Speaker Catalog")]
    public sealed class VnSpeakerCatalogSO : ScriptableObject
    {
        [SerializeField] private List<VnSpeakerDefinitionSO> speakers = new List<VnSpeakerDefinitionSO>();

        public IReadOnlyList<VnSpeakerDefinitionSO> Speakers => speakers;

        public bool TryGet(string speakerId, out VnSpeakerDefinitionSO definition)
        {
            definition = null;
            if (string.IsNullOrWhiteSpace(speakerId) || speakers == null)
            {
                return false;
            }

            for (var i = 0; i < speakers.Count; i++)
            {
                var entry = speakers[i];
                if (entry != null && entry.speakerId == speakerId)
                {
                    definition = entry;
                    return true;
                }
            }

            return false;
        }

        public void EditorReplaceAll(List<VnSpeakerDefinitionSO> next)
        {
            speakers = next ?? new List<VnSpeakerDefinitionSO>();
        }
    }
}
