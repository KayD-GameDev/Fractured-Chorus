using FracturedChorus.Audio;
using FracturedChorus.Hub;
using FracturedChorus.Meta;
using UnityEngine;

namespace FracturedChorus.RunMap
{
    public static class RunMapHubBridge
    {
        public static void ReturnFromRunMapNavigation()
        {
            ReturnToCampusHub(forceTownMap: false);
        }

        public static void ReturnToCampusHub(
            bool consumeEveningSlotIfNeeded = true,
            bool openStatusMenu = false,
            MetaStatusMenuUI.Tab statusMenuTab = MetaStatusMenuUI.Tab.Stats,
            bool forceTownMap = false)
        {
            try
            {
                if (RunMusicSession.Instance != null)
                {
                    RunMusicSession.Instance.Stop();
                }

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

                if (openStatusMenu)
                {
                    TownMapView.PrepareReturnToHub(true, statusMenuTab);
                }
                else
                {
                    HubNavigationEscContext.ApplyBeforeLoadCampusHub(forceTownMap);
                }

                RunMapSceneLoader.LoadByName(RunMapSceneCatalog.CampusHub);
            }
            catch (System.Exception error)
            {
                TownMapView.OpenStatusMenuOnNextShow = false;
                TownMapView.OpenStatusMenuTabOnNextShow = null;
                HubNavigationEscContext.Clear();
                Debug.LogError($"[Fractured Chorus] Return to CampusHub failed: {error}");
            }
        }
    }
}
