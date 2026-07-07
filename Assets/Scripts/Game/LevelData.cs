using System.Collections.Generic;
using GameLift.Levels;
using UnityEngine;
using UnityEngine.Serialization;

namespace TrashRush.Game
{
    [CreateAssetMenu(fileName = "LevelData", menuName = "Trash Rush/Game/Level Data")]
    public sealed class LevelData : BaseLevelData
    {
        [SerializeField] private List<SlashObjectData> _slashObjects = new();
        [Header("Obstacles")]
        [SerializeField] private List<ObstacleObjectData> _obstacleObjects = new();
        [SerializeField, Min(0.01f)] private float _obstacleSpawnInterval = 3f;
        [SerializeField, Min(1)] private int _obstaclesPerSpawn = 1;
        [FormerlySerializedAs("_obstacleSpawnCount")]
        [SerializeField, Min(0)] private int _maxObstacleSpawnCount;

        public IReadOnlyList<SlashObjectData> SlashObjects => _slashObjects;
        public IReadOnlyList<ObstacleObjectData> ObstacleObjects =>
            _obstacleObjects;
        public float ObstacleSpawnInterval =>
            Mathf.Max(0.01f, _obstacleSpawnInterval);
        public int ObstaclesPerSpawn =>
            Mathf.Max(1, _obstaclesPerSpawn);
        public int MaxObstacleSpawnCount =>
            Mathf.Max(0, _maxObstacleSpawnCount);
    }
}
