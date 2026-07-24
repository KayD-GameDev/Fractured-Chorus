using UnityEngine;

namespace FracturedChorus.Hub.FlowerWork
{
    [CreateAssetMenu(
        fileName = "FlowerWorkScenario_",
        menuName = "Fractured Chorus/Hub/Flower Work Scenario")]
    public sealed class FlowerWorkScenarioSO : ScriptableObject
    {
        public string id;
        [TextArea(2, 5)] public string customerLine;
        [TextArea(1, 3)] public string thinkPrompt = "Which flowers fit the request?";
        public string[] choices = new string[3];
        [Range(0, 2)] public int correctIndex;
        [TextArea(1, 4)] public string correctReply;
        [TextArea(1, 4)] public string wrongReply;
    }
}
