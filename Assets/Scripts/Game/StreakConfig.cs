using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Serialization;

namespace TrashRush.Game
{
    [System.Serializable]
    public sealed class StreakColorData
    {
        [SerializeField, Min(1)] private int _streakCount = 1;
        [SerializeField] private Color _color = Color.white;

        public int StreakCount => Mathf.Max(1, _streakCount);
        public Color Color => _color;
    }

    [CreateAssetMenu(fileName = "StreakConfig", menuName = "Trash Rush/Game/Streak Config")]
    public sealed class StreakConfig : ScriptableObject
    {
        [Header("General")]
        [SerializeField, Min(0.01f)] private float _resetDelay = 1.5f;
        [SerializeField] private bool _useUnscaledTime;

        [Header("Streak Text")]
        [SerializeField] private List<StreakColorData> _streakColorLoop = new();

        [Header("Hit Text")]
        [SerializeField, Min(0f)] private float _hitTextScaleIncreasePerStreak = 0.08f;
        [SerializeField, Min(1f)] private float _hitTextMaxScaleMultiplier = 2.5f;

        [Header("Hit Effects")]
        [SerializeField, Min(0f)] private float _rotationStrengthIncreasePerStreak = 0.08f;
        [SerializeField, Min(1f)] private float _maxRotationStrengthMultiplier = 2.5f;
        [SerializeField, Min(0f)] private float _screenShakeStrengthPerStreak = 0.02f;

        [Header("Camera Zoom")]
        [SerializeField] private bool _zoomEnabled = true;
        [SerializeField, Min(0f)] private float _zoomAmountPerHit = 0.08f;
        [SerializeField, Min(0f)] private float _zoomMaxAmount = 1.2f;
        [SerializeField, Min(0.01f)] private float _zoomInDuration = 0.1f;
        [SerializeField, Min(0.01f)] private float _zoomResetDuration = 0.8f;
        [SerializeField] private Ease _zoomInEase = Ease.OutQuad;
        [SerializeField] private Ease _zoomResetEase = Ease.OutSine;

        [Header("Slash Streak")]
        [FormerlySerializedAs("_fireSlashEnabled")]
        [SerializeField] private bool _slashStreakEffectEnabled = true;
        [FormerlySerializedAs("_fireSlashStartStreak")]
        [SerializeField, Min(1)] private int _slashStreakEffectStartStreak = 3;
        [FormerlySerializedAs("_fireSlashLocalOffset")]
        [SerializeField] private Vector2 _slashStreakEffectLocalOffset = new(0.3f, 0f);
        [FormerlySerializedAs("_fireSlashLifetime")]
        [SerializeField, Min(0.01f)] private float _slashStreakEffectLifetime = 1.1f;
        [FormerlySerializedAs("_fireSlashPoolPreload")]
        [SerializeField, Min(0)] private int _slashStreakEffectPoolPreload = 3;

        [Header("Slash Streak Background")]
        [SerializeField] private bool _slashStreakBackgroundEnabled = true;
        [SerializeField, Min(1)] private int _slashStreakBackgroundStartStreak = 3;
        [SerializeField, Range(0f, 1f)] private float _slashStreakBackgroundChance = 1f;

        public float ResetDelay => Mathf.Max(0.01f, _resetDelay);
        public bool UseUnscaledTime => _useUnscaledTime;
        public bool ZoomEnabled => _zoomEnabled;
        public float ZoomAmountPerHit => Mathf.Max(0f, _zoomAmountPerHit);
        public float ZoomMaxAmount => Mathf.Max(0f, _zoomMaxAmount);
        public float ZoomInDuration => Mathf.Max(0.01f, _zoomInDuration);
        public float ZoomResetDuration => Mathf.Max(0.01f, _zoomResetDuration);
        public Ease ZoomInEase => _zoomInEase;
        public Ease ZoomResetEase => _zoomResetEase;
        public Vector2 SlashStreakEffectLocalOffset => _slashStreakEffectLocalOffset;
        public float SlashStreakEffectLifetime =>
            Mathf.Max(0.01f, _slashStreakEffectLifetime);
        public int SlashStreakEffectPoolPreload =>
            Mathf.Max(0, _slashStreakEffectPoolPreload);
        public IReadOnlyList<StreakColorData> StreakColorLoop => _streakColorLoop;

        public Color GetStreakTextColor(int streak, Color fallback)
        {
            if (_streakColorLoop == null || _streakColorLoop.Count == 0)
            {
                return fallback;
            }

            var selectedColor = fallback;
            var selectedStreakCount = 0;

            for (var i = 0; i < _streakColorLoop.Count; i++)
            {
                var data = _streakColorLoop[i];

                if (data == null ||
                    streak < data.StreakCount ||
                    data.StreakCount < selectedStreakCount)
                {
                    continue;
                }

                selectedStreakCount = data.StreakCount;
                selectedColor = data.Color;
            }

            return selectedColor;
        }

        public float GetHitTextScaleMultiplier(int streak)
        {
            var multiplier =
                1f + Mathf.Max(0, streak - 1) * Mathf.Max(0f, _hitTextScaleIncreasePerStreak);
            return Mathf.Min(multiplier, Mathf.Max(1f, _hitTextMaxScaleMultiplier));
        }

        public float GetRotationStrengthMultiplier(int streak)
        {
            var multiplier =
                1f + Mathf.Max(0, streak - 1) * Mathf.Max(0f, _rotationStrengthIncreasePerStreak);
            return Mathf.Min(multiplier, Mathf.Max(1f, _maxRotationStrengthMultiplier));
        }

        public float GetScreenShakeStrength(int streak, float baseStrength, float maxStrength)
        {
            var strength =
                Mathf.Max(0f, baseStrength) +
                Mathf.Max(0, streak - 1) * Mathf.Max(0f, _screenShakeStrengthPerStreak);
            return Mathf.Min(strength, Mathf.Max(baseStrength, maxStrength));
        }

        public bool ShouldSpawnSlashStreakEffect(int streak)
        {
            return _slashStreakEffectEnabled &&
                   streak >= Mathf.Max(1, _slashStreakEffectStartStreak);
        }

        public bool ShouldPlaySlashStreakBackground(int streak)
        {
            return _slashStreakBackgroundEnabled &&
                   streak >= Mathf.Max(1, _slashStreakBackgroundStartStreak) &&
                   Random.value < Mathf.Clamp01(_slashStreakBackgroundChance);
        }
    }
}
