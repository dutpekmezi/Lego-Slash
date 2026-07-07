using GameLift.Buttons;
using GameLift.Popup;
using GameLift.Scene;
using UnityEngine;
using VContainer;

namespace GameLift.UI.GamePopups
{
    public sealed class SettingsGamePopUp : PopupBase
    {
        public const string PopupIdConst = "settings_game_popup";

        [SerializeField] private BaseButton _continueButton;
        [SerializeField] private BaseButton _restartButton;
        [SerializeField] private BaseButton _quitButton;

        private ISceneService _sceneService;

        public override string PopupId => PopupIdConst;

        [Inject]
        private void Construct(
            IObjectResolver resolver,
            ISceneService sceneService)
        {
            _sceneService = sceneService;

            resolver.Inject(_continueButton);
            resolver.Inject(_restartButton);
            resolver.Inject(_quitButton);

            _continueButton.Button.onClick.AddListener(OnContinueClicked);
            _restartButton.Button.onClick.AddListener(OnRestartClicked);
            _quitButton.Button.onClick.AddListener(OnQuitClicked);
        }

        private void OnContinueClicked()
        {
            DisableButtons();
            Disappear();
        }

        private async void OnRestartClicked()
        {
            DisableButtons();
            Disappear();
            await _sceneService.LoadScene(SceneKeys.GameScene);
        }

        private async void OnQuitClicked()
        {
            DisableButtons();
            Disappear();
            await _sceneService.LoadScene(SceneKeys.MenuScene);
        }

        private void DisableButtons()
        {
            _continueButton.Button.interactable = false;
            _restartButton.Button.interactable = false;
            _quitButton.Button.interactable = false;
        }
    }
}
