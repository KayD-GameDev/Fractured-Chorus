using System;
using FracturedChorus.Menu;
using FracturedChorus.Narrative;
using UnityEngine;

namespace FracturedChorus.Narrative.Vn
{
    public sealed class VnConvenienceController : MonoBehaviour
    {
        [SerializeField] private VnConvenienceBarView bar;
        [SerializeField] private VnLogPanelView logPanel;
        [SerializeField] private float autoAdvanceDelay = 2.4f;
        [SerializeField] private float skipAdvanceInterval = 0.02f;

        private VnConvenienceBindings _bindings;
        private bool _autoEnabled;
        private bool _skipEnabled;
        private bool _ctrlSkipHeld;
        private float _autoTimer;
        private float _skipTimer;

        public VnDialogueLog Log => VnDialogueLog.Session;
        public bool AutoEnabled => _autoEnabled;
        public bool SkipEnabled => _skipEnabled || _ctrlSkipHeld;
        public bool LogOpen => logPanel != null && logPanel.IsOpen;

        public void Bind(VnConvenienceBindings bindings)
        {
            _bindings = bindings != null && bindings.IsRunning != null ? bindings : null;
        }

        public void ResetSession()
        {
            VnDialogueLog.Session.Clear();
            _autoEnabled = false;
            _skipEnabled = false;
            _ctrlSkipHeld = false;
            _autoTimer = 0f;
            _skipTimer = 0f;
            bar?.SetAutoActive(false);
            bar?.SetSkipActive(false);
            logPanel?.Hide();
        }

        public void AppendLog(string speaker, string text)
        {
            VnDialogueLog.Session.Append(speaker, text);
            if (LogOpen)
            {
                logPanel?.Refresh(VnDialogueLog.Session);
            }
        }

        public void CloseLog()
        {
            logPanel?.Hide();
        }

        public void SetBarVisible(bool visible)
        {
            bar?.SetVisible(visible);
        }

        private void Awake()
        {
            WireBar();
            EnsureBarOnTop();
        }

        private void OnEnable()
        {
            WireBar();
        }

        private void WireBar()
        {
            if (bar == null)
            {
                bar = GetComponentInChildren<VnConvenienceBarView>(true);
            }

            if (logPanel == null)
            {
                logPanel = GetComponentInChildren<VnLogPanelView>(true);
            }

            if (bar == null)
            {
                return;
            }

            bar.LogClicked -= OnLogClicked;
            bar.AutoClicked -= ToggleAuto;
            bar.SkipClicked -= ToggleSkip;
            bar.LogClicked += OnLogClicked;
            bar.AutoClicked += ToggleAuto;
            bar.SkipClicked += ToggleSkip;
        }

        private void OnDisable()
        {
            _bindings = null;
            StopSkip();
            _autoEnabled = false;
            bar?.SetAutoActive(false);
        }

        private void Update()
        {
            if (LogOpen)
            {
                if (PrologueInput.WasCancelPressedThisFrame() ||
                    PrologueInput.WasKeyboardAdvancePressedThisFrame())
                {
                    CloseLog();
                }

                return;
            }

            if (!InvokeBool(_bindings?.IsRunning))
            {
                return;
            }

            var playbackActive = _bindings.IsPlaybackActive != null
                ? InvokeBool(_bindings.IsPlaybackActive)
                : InvokeBool(_bindings.IsRunning);
            if (!playbackActive)
            {
                if (_skipEnabled || _ctrlSkipHeld)
                {
                    StopSkip();
                }

                return;
            }

            UpdateCtrlSkip();

            if (SkipEnabled)
            {
                TickSkip();
                return;
            }

            if (_autoEnabled)
            {
                TickAuto();
            }
        }

        private void UpdateCtrlSkip()
        {
            var held = PrologueInput.WasSkipHeld();
            if (held && !_ctrlSkipHeld)
            {
                _ctrlSkipHeld = true;
                _autoEnabled = false;
                bar?.SetAutoActive(false);
                bar?.SetSkipActive(true);
                _skipTimer = 0f;
                TrySkipStep();
            }
            else if (!held && _ctrlSkipHeld)
            {
                _ctrlSkipHeld = false;
                if (!_skipEnabled)
                {
                    bar?.SetSkipActive(false);
                }
            }
        }

        private void OnLogClicked()
        {
            if (logPanel == null)
            {
                return;
            }

            if (logPanel.IsOpen)
            {
                logPanel.Hide();
                return;
            }

            if (_skipEnabled || _ctrlSkipHeld)
            {
                StopSkip();
            }

            logPanel.Show(VnDialogueLog.Session);
            EnsureBarOnTop();
        }

        private void EnsureBarOnTop()
        {
            if (bar == null)
            {
                return;
            }

            bar.transform.SetAsLastSibling();
        }

        private void ToggleAuto()
        {
            if (_bindings != null && InvokeBool(_bindings.IsAtSkipStop))
            {
                return;
            }

            if (_skipEnabled || _ctrlSkipHeld)
            {
                StopSkip();
            }

            _autoEnabled = !_autoEnabled;
            _autoTimer = 0f;
            bar?.SetAutoActive(_autoEnabled);
        }

        private void ToggleSkip()
        {
            if (_skipEnabled)
            {
                StopSkip();
                return;
            }

            if (_bindings == null || InvokeBool(_bindings.IsAtSkipStop))
            {
                return;
            }

            _skipEnabled = true;
            _autoEnabled = false;
            bar?.SetAutoActive(false);
            bar?.SetSkipActive(true);
            _skipTimer = 0f;
            TrySkipStep();
        }

        private void StopSkip()
        {
            _skipEnabled = false;
            _ctrlSkipHeld = false;
            bar?.SetSkipActive(false);
        }

        private void TickSkip()
        {
            if (_bindings == null)
            {
                StopSkip();
                return;
            }

            if (InvokeBool(_bindings.IsAtSkipStop))
            {
                StopSkip();
                return;
            }

            if (InvokeBool(_bindings.IsTransitionBusy))
            {
                _bindings.RequestSkipTransition?.Invoke();
                return;
            }

            if (InvokeBool(_bindings.IsTyping))
            {
                _bindings.SkipTyping?.Invoke();
                _skipTimer = 0f;
                if (InvokeBool(_bindings.IsWaitingAdvance))
                {
                    TryAdvanceSkip();
                }

                return;
            }

            if (!InvokeBool(_bindings.IsWaitingAdvance))
            {
                return;
            }

            _skipTimer += Time.unscaledDeltaTime;
            if (_skipTimer < skipAdvanceInterval)
            {
                return;
            }

            _skipTimer = 0f;
            TryAdvanceSkip();
        }

        private void TrySkipStep()
        {
            if (_bindings == null)
            {
                StopSkip();
                return;
            }

            if (InvokeBool(_bindings.IsAtSkipStop))
            {
                StopSkip();
                return;
            }

            if (InvokeBool(_bindings.IsTransitionBusy))
            {
                _bindings.RequestSkipTransition?.Invoke();
                return;
            }

            if (InvokeBool(_bindings.IsTyping))
            {
                _bindings.SkipTyping?.Invoke();
                if (InvokeBool(_bindings.IsWaitingAdvance))
                {
                    TryAdvanceSkip();
                }

                return;
            }

            if (InvokeBool(_bindings.IsWaitingAdvance))
            {
                TryAdvanceSkip();
            }
        }

        private void TryAdvanceSkip()
        {
            if (!CanSkipCurrentLine())
            {
                HaltSkipOnUnread();
                return;
            }

            _bindings?.RequestAdvance?.Invoke();
        }

        private bool CanSkipCurrentLine()
        {
            if (_skipEnabled || MainMenuGameSettings.SkipUnreadText)
            {
                return true;
            }

            if (_bindings?.IsCurrentLineRead == null)
            {
                return true;
            }

            return InvokeBool(_bindings.IsCurrentLineRead);
        }

        private void HaltSkipOnUnread()
        {
            _skipEnabled = false;
            if (!_ctrlSkipHeld)
            {
                bar?.SetSkipActive(false);
            }
        }

        private void TickAuto()
        {
            if (_bindings == null)
            {
                _autoTimer = 0f;
                return;
            }

            if (InvokeBool(_bindings.IsAtSkipStop) || InvokeBool(_bindings.IsTransitionBusy))
            {
                _autoTimer = 0f;
                return;
            }

            if (InvokeBool(_bindings.IsTyping))
            {
                _autoTimer = 0f;
                return;
            }

            if (!InvokeBool(_bindings.IsWaitingAdvance))
            {
                _autoTimer = 0f;
                return;
            }

            _autoTimer += Time.unscaledDeltaTime;
            if (_autoTimer < autoAdvanceDelay)
            {
                return;
            }

            _autoTimer = 0f;
            _bindings.RequestAdvance?.Invoke();
        }

        private static bool InvokeBool(Func<bool> func)
        {
            return func != null && func();
        }
    }

    public sealed class VnConvenienceBindings
    {
        public Func<bool> IsRunning;
        public Func<bool> IsPlaybackActive;
        public Func<bool> IsTyping;
        public Func<bool> IsWaitingAdvance;
        public Func<bool> IsTransitionBusy;
        public Func<bool> IsAtSkipStop;
        public Func<bool> IsCurrentLineRead;
        public Action RequestAdvance;
        public Action SkipTyping;
        public Action RequestSkipTransition;
    }
}
