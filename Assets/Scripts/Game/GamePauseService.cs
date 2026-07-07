using System;
using GameLift.Popup;
using GameLift.UI.SettingsPopup;
using UnityEngine;
using UnityEngine.UI;
using VContainer.Unity;

namespace TrashRush.Game
{
    public sealed class GamePauseService : IStartable, IDisposable
    {
        private readonly SettingsButton _settingsButton;
        private readonly IPopupService _popupService;

        private PopupBase _pausePopup;
        private Image _settingsButtonImage;
        private float _previousTimeScale = 1f;
        private bool _isPaused;

        public bool IsPaused => _isPaused;

        public GamePauseService(
            SettingsButton settingsButton,
            IPopupService popupService)
        {
            _settingsButton = settingsButton;
            _popupService = popupService;
        }

        public void Start()
        {
            if (_settingsButton == null || _settingsButton.Button == null)
            {
                return;
            }

            _settingsButtonImage = _settingsButton.GetComponent<Image>();
            _settingsButton.Button.onClick.AddListener(
                OnSettingsButtonClicked);
        }

        public void Dispose()
        {
            if (_settingsButton != null && _settingsButton.Button != null)
            {
                _settingsButton.Button.onClick.RemoveListener(
                    OnSettingsButtonClicked);
            }

            Resume();
        }

        private void OnSettingsButtonClicked()
        {
            var popup = _popupService.Get(_settingsButton.PopupId);

            if (popup == null || popup == _pausePopup)
            {
                return;
            }

            if (_isPaused)
            {
                Resume();
            }

            _pausePopup = popup;
            _pausePopup.PostDisappear += OnPausePopupClosed;
            _previousTimeScale = Time.timeScale;
            _isPaused = true;
            SetSettingsButtonVisible(false);
            Time.timeScale = 0f;
        }

        private void OnPausePopupClosed()
        {
            Resume();
        }

        private void Resume()
        {
            if (_pausePopup != null)
            {
                _pausePopup.PostDisappear -= OnPausePopupClosed;
                _pausePopup = null;
            }

            if (!_isPaused)
            {
                return;
            }

            Time.timeScale = _previousTimeScale;
            _isPaused = false;
            SetSettingsButtonVisible(true);
        }

        private void SetSettingsButtonVisible(bool visible)
        {
            if (_settingsButtonImage != null)
            {
                _settingsButtonImage.enabled = visible;
            }

            if (_settingsButton != null && _settingsButton.Button != null)
            {
                _settingsButton.Button.interactable = visible;
            }
        }
    }
}
