using UnityEngine;

namespace TrashRush.Game
{
    [CreateAssetMenu(
        fileName = "SlashObjectSpawnConfig",
        menuName = "Trash Rush/Game/Slash Object Spawn Config")]
    public sealed class SlashObjectSpawnConfig : ScriptableObject
    {
        [SerializeField, Range(0f, 1f)] private float _viewportX = 0.5f;
        [SerializeField] private float _spawnViewportY = 1.2f;
        [SerializeField] private float _despawnViewportY = -0.2f;
        [Header("Obstacles")]
        [SerializeField, Range(0f, 1f)] private float _obstacleMinViewportX = 0.1f;
        [SerializeField, Range(0f, 1f)] private float _obstacleMaxViewportX = 0.9f;
        [SerializeField, Range(0f, 1f)] private float _obstacleMinViewportDistance = 0.2f;
        [SerializeField] private bool _useUnscaledTime;

        public float ViewportX => Mathf.Clamp01(_viewportX);
        public float SpawnViewportY => _spawnViewportY;
        public float DespawnViewportY => _despawnViewportY;
        public float ObstacleMinViewportX =>
            Mathf.Min(
                Mathf.Clamp01(_obstacleMinViewportX),
                Mathf.Clamp01(_obstacleMaxViewportX));
        public float ObstacleMaxViewportX =>
            Mathf.Max(
                Mathf.Clamp01(_obstacleMinViewportX),
                Mathf.Clamp01(_obstacleMaxViewportX));
        public float ObstacleMinViewportDistance =>
            Mathf.Clamp01(_obstacleMinViewportDistance);
        public bool UseUnscaledTime => _useUnscaledTime;
    }
}
