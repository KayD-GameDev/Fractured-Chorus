using FracturedChorus.Meta;
using UnityEngine;

namespace FracturedChorus.RunMap
{
    public static class RunMapHubBridge
    {
        public static void ReturnToCampusHub(bool consumeEveningSlotIfNeeded = true)
        {
            try
            {
                var state = GameMetaSession.Current;
                if (state.RunSnapshot.HasActiveRun)
                {
                    state.RunSnapshot.HasActiveRun = false;
                    if (consumeEveningSlotIfNeeded && state.Calendar.CurrentPhase == DayPhase.Evening)
                    {
                        state.ConsumeActivitySlot();
                    }

                    GameMetaSession.Save();
                }

                RunMapSceneLoader.LoadByName(RunMapSceneCatalog.CampusHub);
            }
            catch (System.Exception error)
            {
                Debug.LogError($"[Fractured Chorus] Return to CampusHub failed: {error}");
            }
        }
    }
}
