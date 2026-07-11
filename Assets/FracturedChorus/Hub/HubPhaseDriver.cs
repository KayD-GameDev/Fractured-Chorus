using FracturedChorus.Meta;
using FracturedChorus.RunMap;
using UnityEngine;

namespace FracturedChorus.Hub
{
    public sealed class HubPhaseDriver
    {
        private readonly MorningBeatUI _morningUi;
        private readonly TownMapView _townMap;
        private readonly CalendarUIView _calendarUi;
        private readonly CalendarSlashBanner _slashBanner;
        private readonly CampusHubController _host;

        public HubPhaseDriver(
            CampusHubController host,
            MorningBeatUI morningUi,
            TownMapView townMap,
            CalendarUIView calendarUi,
            CalendarSlashBanner slashBanner)
        {
            _host = host;
            _morningUi = morningUi;
            _townMap = townMap;
            _calendarUi = calendarUi;
            _slashBanner = slashBanner;
        }

        public void BeginCurrentPhase()
        {
            var state = GameMetaSession.Current;
            RefreshCalendar(state);

            if (state.Calendar.IsArcComplete)
            {
                _host.ShowStatus("Arc 1 kết thúc (30/09). Phase 6 sẽ có summary.");
                _morningUi?.Hide();
                _townMap?.Hide();
                return;
            }

            switch (state.Calendar.CurrentPhase)
            {
                case DayPhase.Morning:
                    BeginMorning(state);
                    break;
                case DayPhase.Day:
                case DayPhase.Evening:
                    BeginTownMap(state);
                    break;
            }
        }

        private void BeginMorning(GameMetaState state)
        {
            _townMap?.Hide();

            if (_morningUi == null)
            {
                Debug.LogError("[Fractured Chorus] MorningBeatUI missing — skip to town map. Re-run Setup CampusHub Scene Hierarchy (Edit Mode).");
                CompleteMorningAndEnterTownMap(state);
                return;
            }

            _morningUi.Show(state, () =>
            {
                try
                {
                    CompleteMorningAndEnterTownMap(state);
                }
                catch (System.Exception error)
                {
                    Debug.LogError($"[Fractured Chorus] Morning beat failed: {error}");
                    _host.ShowStatus("Không thể tiếp tục buổi sáng. Thử lại.");
                }
            });
        }

        private void CompleteMorningAndEnterTownMap(GameMetaState state)
        {
            CampusHubStoryBeats.ApplyMorningFlags(state);
            state.CompleteMorningQuiz();
            GameMetaSession.Save();
            BeginTownMap(GameMetaSession.Current);
        }

        private void BeginTownMap(GameMetaState state)
        {
            _morningUi?.Hide();
            RefreshCalendar(state);

            if (_townMap == null)
            {
                Debug.LogError("[Fractured Chorus] TownMapView missing — re-run Setup CampusHub Scene Hierarchy (Edit Mode).");
                _host.ShowStatus("Town Map chưa được gán. Setup lại scene ở Edit Mode.");
                return;
            }

            _townMap.Show(state, state.Calendar.CurrentPhase, OnActivityChosen);
        }

        private void OnActivityChosen(string activityId)
        {
            try
            {
                var state = GameMetaSession.Current;
                if (!HubActivityCatalog.TryGet(activityId, state.Calendar.CurrentPhase, out var option))
                {
                    _host.ShowStatus($"Activity '{activityId}' chưa sẵn sàng.");
                    return;
                }

                if (option.Id == "dungeon_run")
                {
                    option.Apply(state);
                    GameMetaSession.Save();
                    RunMapSceneLoader.LoadByName(RunMapSceneCatalog.RunMapPrototype);
                    return;
                }

                option.Apply(state);
                var dayEnded = state.ConsumeActivitySlot();
                GameMetaSession.Save();

                RefreshCalendar(state);

                if (dayEnded)
                {
                    _host.ShowStatus($"Ngày mới: {state.Calendar.CurrentDate.ToDisplayString()}");
                }

                BeginCurrentPhase();
            }
            catch (System.Exception error)
            {
                Debug.LogError($"[Fractured Chorus] Activity failed: {error}");
                _host.ShowStatus("Không thể thực hiện hoạt động.");
            }
        }

        private void RefreshCalendar(GameMetaState state)
        {
            _calendarUi?.Refresh(state);
            _slashBanner?.Refresh(state);
            _townMap?.RefreshCalendar(state);
        }
    }
}
