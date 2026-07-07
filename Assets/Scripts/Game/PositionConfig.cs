using UnityEngine;
using UnityEngine.Serialization;

namespace TrashRush.Game
{
    [CreateAssetMenu(fileName = "PositionConfig", menuName = "Trash Rush/Game/Position Config")]
    public sealed class PositionConfig : ScriptableObject
    {
        [SerializeField] private float _slashZ = -2f;
        [FormerlySerializedAs("_fireSlashZ")]
        [SerializeField] private float _slashStreakEffectZ = -1f;
        [SerializeField] private float _woodZ = 33.4f;
        [SerializeField] private float _obstacleZ = 32.4f;
        [SerializeField] private float _backgroundZ = 100f;
        [SerializeField] private float _fallingLeavesZ = 96.98f;

        public float SlashZ => _slashZ;
        public float SlashStreakEffectZ => _slashStreakEffectZ;
        public float WoodZ => _woodZ;
        public float ObstacleZ => _obstacleZ;
        public float BackgroundZ => _backgroundZ;
        public float FallingLeavesZ => _fallingLeavesZ;
    }
}
