using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using VContainer.Unity;

namespace TrashRush.Game
{
    public sealed class ScoreView : IStartable, IDisposable
    {
        private readonly ScoreSystem _scoreSystem;
        private readonly TMP_Text _label;

        private Tween _punchTween;
        private Vector3 _initialScale;

        public ScoreView(ScoreSystem scoreSystem, TMP_Text label)
        {
            _scoreSystem = scoreSystem;
            _label = label;
        }

        public void Start()
        {
            if (_label != null)
            {
                _initialScale = _label.rectTransform.localScale;
            }

            if (_scoreSystem == null)
            {
                SetScore(0);
                return;
            }

            _scoreSystem.ScoreChanged += OnScoreChanged;
            SetScore(_scoreSystem.CurrentScore);
        }

        public void Dispose()
        {
            if (_scoreSystem != null)
            {
                _scoreSystem.ScoreChanged -= OnScoreChanged;
            }

            if (_punchTween != null && _punchTween.IsActive())
            {
                _punchTween.Kill(false);
            }

            if (_label != null)
            {
                _label.rectTransform.localScale = _initialScale;
            }
        }

        private void OnScoreChanged(int score)
        {
            SetScore(score);

            if (_label == null)
            {
                return;
            }

            if (_punchTween != null && _punchTween.IsActive())
            {
                _punchTween.Kill(false);
            }

            _label.rectTransform.localScale = _initialScale;
            _punchTween = _label.rectTransform
                .DOPunchScale(Vector3.one * 0.2f, 0.25f, 6, 0.8f)
                .SetLink(_label.gameObject)
                .OnKill(() => _punchTween = null);
        }

        private void SetScore(int score)
        {
            if (_label != null)
            {
                _label.SetText("{0}", score);
            }
        }
    }
}
