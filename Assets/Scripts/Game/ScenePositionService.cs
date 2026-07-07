using UnityEngine;
using VContainer.Unity;

namespace TrashRush.Game
{
    public sealed class ScenePositionService : IStartable
    {
        private readonly PositionConfig _config;
        private readonly Transform _background;
        private readonly EffectBase _fallingLeaves;

        public ScenePositionService(
            PositionConfig config,
            Transform background,
            EffectBase fallingLeaves)
        {
            _config = config;
            _background = background;
            _fallingLeaves = fallingLeaves;
        }

        public void Start()
        {
            if (_config == null)
            {
                return;
            }

            SetWorldZ(_background, _config.BackgroundZ);

            if (_fallingLeaves != null)
            {
                SetWorldZ(_fallingLeaves.transform, _config.FallingLeavesZ);
            }
        }

        private static void SetWorldZ(Transform target, float worldZ)
        {
            if (target == null)
            {
                return;
            }

            var position = target.position;
            position.z = worldZ;
            target.position = position;
        }
    }
}
