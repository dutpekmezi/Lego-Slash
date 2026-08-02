using UnityEngine;

namespace TrashRush.Game
{
    [CreateAssetMenu(fileName = "HitEffectConfig", menuName = "Trash Rush/Game/Hit Effect Config")]
    public sealed class HitEffectConfig : ScriptableObject
    {
        [SerializeField] private bool _screenShakeEnabled = true;
        [SerializeField, Min(0f)] private float _screenShakeDuration = 0.12f;
        [SerializeField, Min(0f)] private float _screenShakeBaseStrength = 0.06f;
        [SerializeField, Min(0f)] private float _screenShakeMaxStrength = 0.3f;
        [SerializeField, Min(1)] private int _screenShakeVibrato = 10;
        [SerializeField, Range(0f, 180f)] private float _screenShakeRandomness = 90f;
        [Header("Leaf Slash Wind")]
        [SerializeField, Min(0f)] private float _leafWindRadius = 3f;
        [SerializeField, Min(0f)] private float _leafWindVelocityImpulse = 5f;
        [SerializeField, Min(0f)] private float _leafWindMaxVelocity = 10f;
        [SerializeField] private bool _useUnscaledTime;

        public bool ScreenShakeEnabled => _screenShakeEnabled;
        public float ScreenShakeDuration => Mathf.Max(0f, _screenShakeDuration);
        public float ScreenShakeBaseStrength => Mathf.Max(0f, _screenShakeBaseStrength);
        public float ScreenShakeMaxStrength =>
            Mathf.Max(ScreenShakeBaseStrength, _screenShakeMaxStrength);
        public int ScreenShakeVibrato => Mathf.Max(1, _screenShakeVibrato);
        public float ScreenShakeRandomness => Mathf.Clamp(_screenShakeRandomness, 0f, 180f);
        public float LeafWindRadius => Mathf.Max(0f, _leafWindRadius);
        public float LeafWindVelocityImpulse => Mathf.Max(0f, _leafWindVelocityImpulse);
        public float LeafWindMaxVelocity => Mathf.Max(0f, _leafWindMaxVelocity);
        public LeafWindData LeafWindData => new(
            LeafWindRadius,
            LeafWindVelocityImpulse,
            LeafWindMaxVelocity);
        public bool UseUnscaledTime => _useUnscaledTime;
    }
}
