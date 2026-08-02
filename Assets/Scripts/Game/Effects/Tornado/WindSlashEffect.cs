using UnityEngine;

namespace TrashRush.Game
{
    public sealed class WindSlashEffect : EffectBase
    {
        [Header("Streak Shape Scale")]
        [SerializeField, Min(0f)] private float _noStreakShapeScaleY = 1f;
        [SerializeField, Min(0f)] private float _shapeScaleYPerStreak = 0.2f;
        [SerializeField, Min(0f)] private float _maxShapeScaleY = 8f;

        public int CurrentStreak { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            ApplyStreakShapeScale();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            SetStreak(0);
        }

        protected override void OnValidate()
        {
            base.OnValidate();

            _noStreakShapeScaleY = Mathf.Max(0f, _noStreakShapeScaleY);
            _shapeScaleYPerStreak = Mathf.Max(0f, _shapeScaleYPerStreak);
            _maxShapeScaleY = Mathf.Max(_noStreakShapeScaleY, _maxShapeScaleY);

            ApplyStreakShapeScale();
        }

        public void SetStreak(int streak)
        {
            CurrentStreak = Mathf.Max(0, streak);
            ApplyStreakShapeScale();
        }

        public void ResetStreakScale()
        {
            SetStreak(0);
        }

        private void ApplyStreakShapeScale()
        {
            var targetScaleY = CurrentStreak <= 0
                ? _noStreakShapeScaleY
                : _noStreakShapeScaleY + CurrentStreak * _shapeScaleYPerStreak;
            targetScaleY = Mathf.Min(targetScaleY, _maxShapeScaleY);

            var particleSystems = ParticleSystems;

            if (particleSystems == null)
            {
                return;
            }

            for (var i = 0; i < particleSystems.Length; i++)
            {
                var particleSystem = particleSystems[i];

                if (particleSystem == null)
                {
                    continue;
                }

                var shape = particleSystem.shape;
                var scale = shape.scale;
                scale.y = targetScaleY;
                shape.scale = scale;
            }
        }
    }
}
