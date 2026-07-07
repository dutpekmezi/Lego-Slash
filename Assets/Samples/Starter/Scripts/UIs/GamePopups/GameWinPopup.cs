using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using GameLift.Buttons;
using GameLift.Currency;
using GameLift.ObjectFlowAnimator;
using GameLift.Popup;
using GameLift.Scene;
using GameLift.UI.Currency;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace GameLift.UI.GamePopups
{
    public class GameWinPopup : PopupBase
    {
        [SerializeField] private BaseButton _claimButton;
        [SerializeField] private Image _illustration;
        [SerializeField] private Image _slashImage;
        [SerializeField, Min(1)] private int _goldReward = 40;
        [SerializeField, Min(1)] private int _goldPerParticle = 2;

        [Header("Score Scale Punch")]
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField, Min(0f)] private float _scoreRevealDelay = 0.1f;
        [SerializeField, Min(0.01f)] private float _scoreScaleDuration = 0.25f;
        [SerializeField] private Ease _scoreScaleEase = Ease.OutBack;
        [SerializeField, Min(0f)] private float _scorePunchStrength = 0.2f;
        [SerializeField, Min(0.01f)] private float _scorePunchDuration = 0.2f;
        [SerializeField, Range(1, 20)] private int _scorePunchVibrato = 6;
        [SerializeField, Range(0f, 1f)] private float _scorePunchElasticity = 0.8f;
        [SerializeField] private bool _scoreUseUnscaledTime;

        [Header("Score Count Up")]
        [SerializeField, Min(0.01f)] private float _scoreCountDuration = 0.5f;
        [SerializeField] private Ease _scoreCountEase = Ease.Linear;
        [SerializeField, Min(0f)] private float _scoreCountCompletePunchStrength = 0.35f;
        [SerializeField, Min(0.01f)] private float _scoreCountCompletePunchDuration = 0.25f;
        [SerializeField, Range(1, 20)] private int _scoreCountCompletePunchVibrato = 8;
        [SerializeField, Range(0f, 1f)] private float _scoreCountCompletePunchElasticity = 0.9f;

        private ICurrencyService _currencyService;
        private IUIFlowAnimator _uiFlowAnimator;
        private ISceneService _sceneService;

        private Vector3 _scoreInitialScale;
        private Tween _scoreRevealTween;
        private Tween _scoreCountTween;
        private Tween _scoreCountCompletePunchTween;
        private int _finalScore;

        private const string WinPopupFlyGoldKey = "win_popup_fly_gold";

        public override string PopupId => PopupIdConst;
        public const string PopupIdConst = "game_win_popup";


        [Inject]
        private void Construct(ICurrencyService currencyService, IUIFlowAnimator uIFlowAnimator, IObjectResolver resolver, ISceneService sceneService)
        {
            _currencyService = currencyService;
            _uiFlowAnimator = uIFlowAnimator;
            _sceneService = sceneService;

            resolver.Inject(_claimButton);
            foreach (var currencyBar in GetComponentsInChildren<CurrencyBar>(true))
            {
                resolver.Inject(currencyBar);
            }

            if (_slashImage != null)
            {
                _slashImage.gameObject.SetActive(false);
            }

            PrepareScoreReveal();
            PostAppear += PlayScoreReveal;
            _claimButton.Button.onClick.AddListener(OnClaimButtonClicked);
        }

        private void OnDestroy()
        {
            PostAppear -= PlayScoreReveal;
            KillScoreRevealTween();
        }

        private void OnClaimButtonClicked()
        {
            _claimButton.Button.interactable = false;

            if (_slashImage != null)
            {
                _slashImage.gameObject.SetActive(true);
            }

            _ = AnimateRewards();
        }

        public void SetFinalScore(int score)
        {
            _finalScore = Mathf.Max(0, score);

            if (_scoreText != null)
            {
                _scoreText.SetText("0");
            }
        }

        private void PrepareScoreReveal()
        {
            if (_scoreText == null)
            {
                return;
            }

            _scoreInitialScale = _scoreText.rectTransform.localScale;
            _scoreText.rectTransform.localScale = Vector3.zero;
            _scoreText.SetText("0");
        }

        private void PlayScoreReveal()
        {
            KillScoreRevealTween();

            if (_scoreText == null)
            {
                return;
            }

            _scoreText.rectTransform.localScale = Vector3.zero;
            _scoreText.SetText("0");

            if (_scoreRevealDelay <= 0f)
            {
                PlayScoreScale();
                return;
            }

            _scoreRevealTween = DOVirtual
                .DelayedCall(_scoreRevealDelay, PlayScoreScale)
                .SetUpdate(_scoreUseUnscaledTime)
                .SetLink(gameObject);
        }

        private void PlayScoreScale()
        {
            var target = _scoreText.rectTransform;
            target.localScale = Vector3.zero;
            PlayScoreCountUp();

            _scoreRevealTween = target
                .DOScale(_scoreInitialScale, _scoreScaleDuration)
                .SetEase(_scoreScaleEase)
                .SetUpdate(_scoreUseUnscaledTime)
                .SetLink(gameObject)
                .OnComplete(PlayScorePunch);
        }

        private void PlayScorePunch()
        {
            if (_scorePunchStrength <= 0f)
            {
                _scoreRevealTween = null;
                return;
            }

            var target = _scoreText.rectTransform;
            var punch = new Vector3(
                _scorePunchStrength,
                _scorePunchStrength,
                0f);

            _scoreRevealTween = target
                .DOPunchScale(
                    punch,
                    _scorePunchDuration,
                    _scorePunchVibrato,
                    _scorePunchElasticity)
                .SetUpdate(_scoreUseUnscaledTime)
                .SetLink(gameObject)
                .OnComplete(() => _scoreRevealTween = null);
        }

        private void PlayScoreCountUp()
        {
            if (_scoreCountTween != null && _scoreCountTween.IsActive())
            {
                _scoreCountTween.Kill(false);
            }

            if (_finalScore <= 0)
            {
                _scoreText.SetText("0");
                _scoreCountTween = null;
                return;
            }

            _scoreCountTween = DOVirtual
                .Int(
                    0,
                    _finalScore,
                    _scoreCountDuration,
                    value => _scoreText.SetText("{0}", value))
                .SetEase(_scoreCountEase)
                .SetUpdate(_scoreUseUnscaledTime)
                .SetLink(gameObject)
                .OnComplete(() =>
                {
                    _scoreText.SetText("{0}", _finalScore);
                    _scoreCountTween = null;
                    PlayScoreCountCompletePunch();
                });
        }

        private void PlayScoreCountCompletePunch()
        {
            if (_scoreCountCompletePunchStrength <= 0f)
            {
                return;
            }

            var punch = new Vector3(
                _scoreCountCompletePunchStrength,
                _scoreCountCompletePunchStrength,
                0f);

            _scoreCountCompletePunchTween = _scoreText.rectTransform
                .DOPunchScale(
                    punch,
                    _scoreCountCompletePunchDuration,
                    _scoreCountCompletePunchVibrato,
                    _scoreCountCompletePunchElasticity)
                .SetUpdate(_scoreUseUnscaledTime)
                .SetLink(gameObject)
                .OnComplete(() => _scoreCountCompletePunchTween = null);
        }

        private void KillScoreRevealTween()
        {
            if (_scoreRevealTween != null && _scoreRevealTween.IsActive())
            {
                _scoreRevealTween.Kill(false);
            }

            _scoreRevealTween = null;

            if (_scoreCountTween != null && _scoreCountTween.IsActive())
            {
                _scoreCountTween.Kill(false);
            }

            _scoreCountTween = null;

            if (_scoreCountCompletePunchTween != null &&
                _scoreCountCompletePunchTween.IsActive())
            {
                _scoreCountCompletePunchTween.Kill(false);
            }

            _scoreCountCompletePunchTween = null;
        }

        private async UniTask AnimateRewards()
        {
            if (TryFlyGold(_goldReward))
            {
                await UniTask.WaitUntil(() => !_uiFlowAnimator.IsPlaying(WinPopupFlyGoldKey));
            }

            Disappear();
            await _sceneService.LoadScene(SceneKeys.GameScene);
        }

        private bool TryFlyGold(int gold)
        {
            if (gold <= 0)
            {
                return false;
            }

            var currencyBars = FindObjectsByType<CurrencyBar>(FindObjectsSortMode.None);
            var currencyBar = currencyBars.FirstOrDefault(
                bar => bar.isActiveAndEnabled && bar.CurrencyId == CurrencyIds.Gold);
            var currencyConfig = _currencyService.GetCurrencyConfig(CurrencyIds.Gold);

            if (currencyBar == null || currencyConfig == null || _uiFlowAnimator == null)
            {
                _currencyService.ModifyCurrency(CurrencyIds.Gold, gold);
                return false;
            }

            var amountPerParticle = Mathf.Max(1, _goldPerParticle);
            var particleCount = Mathf.Max(1, Mathf.CeilToInt((float)gold / amountPerParticle));
            var remainingGold = gold;
            var startPosition = _illustration != null
                ? _illustration.transform.position
                : _claimButton.transform.position;

            _currencyService.ModifyCurrency(CurrencyIds.Gold, gold, addFakeDecrease: true);

            _uiFlowAnimator.AddNewDestinationAction(
                WinPopupFlyGoldKey,
                startScreenPos: startPosition,
                endScreenPos: currencyBar.transform.position,
                sprite: currencyConfig.currencySprite,
                particleCount: particleCount,
                onReceivedItem: () =>
                {
                    var receivedGold = Mathf.Min(amountPerParticle, remainingGold);
                    remainingGold -= receivedGold;
                    _currencyService.AddFakeCurrency(currencyConfig.currencyId, receivedGold);

                    if (currencyBar != null)
                    {
                        currencyBar.Jump();
                    }
                },
                onCompleted: () =>
                {
                    _currencyService.ClearFakeCurreny(CurrencyIds.Gold);
                }
            );

            return true;
        }
    }
}
