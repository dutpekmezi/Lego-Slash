using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using VContainer.Unity;

namespace TrashRush.Game
{
    public sealed class StreakView : IStartable, IDisposable
    {
        private readonly SlashManager _slashManager;
        private readonly StreakConfig _config;
        private readonly TMP_Text _label;

        private Tween _punchTween;
        private Vector3 _initialScale;
        private Color _fallbackColor;

        public StreakView(
            SlashManager slashManager,
            StreakConfig config,
            TMP_Text label)
        {
            _slashManager = slashManager;
            _config = config;
            _label = label;
        }

        public void Start()
        {
            if (_label != null)
            {
                _initialScale = _label.rectTransform.localScale;
                _fallbackColor = _label.color;
            }

            if (_slashManager == null)
            {
                Hide();
                return;
            }

            _slashManager.StreakChanged += OnStreakChanged;
            SetStreak(_slashManager.CurrentStreak, false);
        }

        public void Dispose()
        {
            if (_slashManager != null)
            {
                _slashManager.StreakChanged -= OnStreakChanged;
            }

            KillPunch();

            if (_label != null)
            {
                _label.rectTransform.localScale = _initialScale;
            }
        }

        private void OnStreakChanged(int streak)
        {
            SetStreak(streak, true);
        }

        private void SetStreak(int streak, bool animate)
        {
            if (_label == null)
            {
                return;
            }

            if (streak <= 0)
            {
                Hide();
                return;
            }

            _label.gameObject.SetActive(true);
            _label.SetText("{0}!", streak);
            _label.color = _config != null
                ? _config.GetStreakTextColor(streak, _fallbackColor)
                : _fallbackColor;

            KillPunch();
            _label.rectTransform.localScale = _initialScale;

            if (!animate)
            {
                return;
            }

            _punchTween = _label.rectTransform
                .DOPunchScale(Vector3.one * 0.2f, 0.25f, 6, 0.8f)
                .SetLink(_label.gameObject)
                .OnKill(() => _punchTween = null);
        }

        private void Hide()
        {
            if (_label == null)
            {
                return;
            }

            KillPunch();
            _label.rectTransform.localScale = _initialScale;
            _label.gameObject.SetActive(false);
        }

        private void KillPunch()
        {
            if (_punchTween != null && _punchTween.IsActive())
            {
                _punchTween.Kill(false);
            }

            _punchTween = null;
        }
    }
}
