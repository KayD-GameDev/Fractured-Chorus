using FracturedChorus.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace FracturedChorus.Hub
{
    public sealed class CampusHubController : MonoBehaviour
    {
        [SerializeField] private Image backgroundImage;
        [SerializeField] private CalendarUIView calendarView;
        [SerializeField] private CalendarSlashBanner slashBanner;
        [SerializeField] private MorningBeatUI morningBeatUi;
        [SerializeField] private TownMapView townMapView;
        [SerializeField] private Text statusLabel;
        [SerializeField] private bool beginHubAfterPrologue = true;

        private HubPhaseDriver _phaseDriver;

        private void Awake()
        {
            ResolveMissingRefs();
            _phaseDriver = new HubPhaseDriver(this, morningBeatUi, townMapView, calendarView, slashBanner);
        }

        private void Start()
        {
            try
            {
                EnsureSession();
                if (GameMetaSession.Current.RunSnapshot.HasActiveRun)
                {
                    GameMetaSession.Current.RunSnapshot.HasActiveRun = false;
                    GameMetaSession.Save();
                }

                _phaseDriver.BeginCurrentPhase();
            }
            catch (System.Exception error)
            {
                Debug.LogError($"[Fractured Chorus] CampusHub start failed: {error}");
                ShowStatus("Không thể khởi tạo campus hub.");
            }
        }

        public void ShowStatus(string message)
        {
            if (statusLabel != null)
            {
                statusLabel.text = message;
            }
        }

        private void ResolveMissingRefs()
        {
            if (morningBeatUi == null)
            {
                morningBeatUi = GetComponentInChildren<MorningBeatUI>(true);
            }

            if (townMapView == null)
            {
                townMapView = GetComponentInChildren<TownMapView>(true);
            }

            if (slashBanner == null)
            {
                slashBanner = GetComponentInChildren<CalendarSlashBanner>(true);
            }

            if (calendarView == null)
            {
                calendarView = GetComponentInChildren<CalendarUIView>(true);
            }

            if (statusLabel == null)
            {
                var status = transform.Find("CampusHubCanvas/StatusLabel");
                if (status != null)
                {
                    statusLabel = status.GetComponent<Text>();
                }
            }
        }

        private void EnsureSession()
        {
            if (!GameMetaSession.HasSession)
            {
                if (beginHubAfterPrologue)
                {
                    GameMetaSession.BeginHubAfterPrologue();
                }
                else
                {
                    GameMetaSession.Load();
                }

                return;
            }

            GameMetaSession.Load();
        }
    }
}
