using System.Collections.Generic;
using UnityEngine;

namespace FracturedChorus.Data
{
    [CreateAssetMenu(fileName = "EventChoiceTable", menuName = "Fractured Chorus/Event Choice Table")]
    public sealed class EventChoiceTableSO : ScriptableObject
    {
        public const string ResourcesPath = "Events/EventChoiceTable_Default";
        public const string AssetPath = "Assets/FracturedChorus/Resources/Events/EventChoiceTable_Default.asset";

        [SerializeField] private EventChoiceSO[] choices;
        [SerializeField, Min(1)] private int offerCount = 3;

        public IReadOnlyList<EventChoiceSO> Choices => choices;
        public int OfferCount => offerCount;

        public EventChoiceSO[] PickOffers(int seed)
        {
            return SeededOfferPicker.Pick(choices, seed, offerCount);
        }

        public static EventChoiceTableSO LoadOrCreateDefault()
        {
            var loaded = Resources.Load<EventChoiceTableSO>(ResourcesPath);
            if (loaded != null && loaded.choices != null && loaded.choices.Length > 0)
            {
                return loaded;
            }

            return CreateRuntimeDefault();
        }

        public static EventChoiceTableSO CreateRuntimeDefault()
        {
            var table = CreateInstance<EventChoiceTableSO>();
            table.EditorAssign(EventChoiceSO.CreateDefaultCatalog(), 3);
            return table;
        }

        public void EditorAssign(EventChoiceSO[] pool, int count)
        {
            choices = pool;
            offerCount = Mathf.Max(1, count);
        }
    }
}
