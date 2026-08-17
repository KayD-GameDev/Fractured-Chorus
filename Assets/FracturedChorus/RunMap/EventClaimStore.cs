using System.Collections.Generic;
using FracturedChorus.Data;

namespace FracturedChorus.RunMap
{
    public static class EventClaimStore
    {
        public readonly struct Claim
        {
            public Claim(string choiceId, string title, EventChoiceKind kind, float magnitude, int nodeId, int floor)
            {
                ChoiceId = choiceId;
                Title = title;
                Kind = kind;
                Magnitude = magnitude;
                NodeId = nodeId;
                Floor = floor;
            }

            public string ChoiceId { get; }
            public string Title { get; }
            public EventChoiceKind Kind { get; }
            public float Magnitude { get; }
            public int NodeId { get; }
            public int Floor { get; }
        }

        private static readonly List<Claim> Claims = new List<Claim>();

        public static IReadOnlyList<Claim> All => Claims;
        public static Claim? Last { get; private set; }

        public static void Record(EventChoiceSO choice, int nodeId, int floor)
        {
            if (choice == null)
            {
                return;
            }

            var claim = new Claim(choice.Id, choice.Title, choice.Kind, choice.Magnitude, nodeId, floor);
            Claims.Add(claim);
            Last = claim;
        }

        public static void ClearRun()
        {
            Claims.Clear();
            Last = null;
        }
    }
}
