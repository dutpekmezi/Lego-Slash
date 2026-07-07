using DG.Tweening;
using UnityEngine;

namespace TrashRush.Game
{
    [CreateAssetMenu(fileName = "HitEffectConfig", menuName = "Trash Rush/Game/Hit Effect Config")]
    public sealed class HitEffectConfig : ScriptableObject
    {
        [SerializeField] private bool _rotationEnabled = true;
        [SerializeField, Min(0f)] private float _rotationDuration = 0.2f;
        [SerializeField, Min(0f)] private float _minRotationAngle = 4f;
        [SerializeField, Min(0f)] private float _maxRotationAngle = 12f;
        [SerializeField] private Ease _rotationEase = Ease.OutSine;
        [SerializeField] private bool _screenShakeEnabled = true;
        [SerializeField, Min(0f)] private float _screenShakeDuration = 0.12f;
        [SerializeField, Min(0f)] private float _screenShakeBaseStrength = 0.06f;
        [SerializeField, Min(0f)] private float _screenShakeMaxStrength = 0.3f;
        [SerializeField, Min(1)] private int _screenShakeVibrato = 10;
        [SerializeField, Range(0f, 180f)] private float _screenShakeRandomness = 90f;
        [SerializeField] private bool _useUnscaledTime;

        public bool RotationEnabled => _rotationEnabled;
        public float RotationDuration => Mathf.Max(0f, _rotationDuration);
        public float MinRotationAngle => Mathf.Max(0f, _minRotationAngle);
        public float MaxRotationAngle => Mathf.Max(MinRotationAngle, _maxRotationAngle);
        public Ease RotationEase => _rotationEase;
        public bool ScreenShakeEnabled => _screenShakeEnabled;
        public float ScreenShakeDuration => Mathf.Max(0f, _screenShakeDuration);
        public float ScreenShakeBaseStrength => Mathf.Max(0f, _screenShakeBaseStrength);
        public float ScreenShakeMaxStrength =>
            Mathf.Max(ScreenShakeBaseStrength, _screenShakeMaxStrength);
        public int ScreenShakeVibrato => Mathf.Max(1, _screenShakeVibrato);
        public float ScreenShakeRandomness => Mathf.Clamp(_screenShakeRandomness, 0f, 180f);
        public bool UseUnscaledTime => _useUnscaledTime;
    }
}
