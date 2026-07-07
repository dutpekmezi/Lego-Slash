using GameLift.Levels;
using GameLift.Popup;
using GameLift.UI.GamePopups;
using UnityEngine;

namespace TrashRush.Game
{
    public sealed class SlashGameResultService
    {
        private readonly LevelService<BaseLevelData> _levelService;
        private readonly IPopupService _popupService;
        private readonly ScoreSystem _scoreSystem;
        private readonly GameObject _scoreCanvas;

        private bool _resultShown;

        public SlashGameResultService(
            LevelService<BaseLevelData> levelService,
            IPopupService popupService,
            ScoreSystem scoreSystem,
            GameObject scoreCanvas)
        {
            _levelService = levelService;
            _popupService = popupService;
            _scoreSystem = scoreSystem;
            _scoreCanvas = scoreCanvas;
        }

        public void ShowWin()
        {
            if (_resultShown)
            {
                return;
            }

            _resultShown = true;
            HideScoreCanvas();
            _levelService.OnLevelCompleted(true);
            var popup = _popupService.Create<GameWinPopup>(true);
            popup.SetFinalScore(_scoreSystem.CurrentScore);
        }

        public void ShowLose()
        {
            if (_resultShown)
            {
                return;
            }

            _resultShown = true;
            HideScoreCanvas();
            _levelService.OnLevelCompleted(false);
            _popupService.Create<GameLosePopup>(true);
        }

        private void HideScoreCanvas()
        {
            if (_scoreCanvas != null)
            {
                _scoreCanvas.SetActive(false);
            }
        }
    }
}
