using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace TrashRush.Game
{
    public sealed class ObstacleObjectSpawnService : IDisposable
    {
        private const int PositionAttemptCount = 12;

        private readonly Camera _camera;
        private readonly SlashObjectSpawnConfig _config;
        private readonly PositionConfig _positionConfig;
        private readonly Transform _parent;
        private readonly List<ObstacleObject> _activeObstacles = new();
        private readonly Dictionary<ObstacleObject, Tween> _fallTweens = new();

        public ObstacleObjectSpawnService(
            Camera camera,
            SlashObjectSpawnConfig config,
            PositionConfig positionConfig,
            Transform parent)
        {
            _camera = camera;
            _config = config;
            _positionConfig = positionConfig;
            _parent = parent;
        }

        public bool TrySpawn(
            IReadOnlyList<ObstacleObjectData> obstacleDatas,
            SlashObject slashObject)
        {
            if (_camera == null ||
                _config == null ||
                _positionConfig == null ||
                slashObject == null ||
                obstacleDatas == null ||
                obstacleDatas.Count == 0)
            {
                return false;
            }

            var validDataCount = 0;

            for (var i = 0; i < obstacleDatas.Count; i++)
            {
                if (obstacleDatas[i] != null &&
                    obstacleDatas[i].Prefab != null)
                {
                    validDataCount++;
                }
            }

            if (validDataCount == 0)
            {
                return false;
            }

            var selectedIndex = UnityEngine.Random.Range(0, validDataCount);

            for (var i = 0; i < obstacleDatas.Count; i++)
            {
                var data = obstacleDatas[i];

                if (data == null || data.Prefab == null)
                {
                    continue;
                }

                if (selectedIndex-- > 0)
                {
                    continue;
                }

                Spawn(data, slashObject);
                return true;
            }

            return false;
        }

        public void Stop(ObstacleObject obstacle)
        {
            if (obstacle == null)
            {
                return;
            }

            KillFallTween(obstacle);
            _activeObstacles.Remove(obstacle);
        }

        public void Dispose()
        {
            for (var i = _activeObstacles.Count - 1; i >= 0; i--)
            {
                var obstacle = _activeObstacles[i];
                KillFallTween(obstacle);

                if (obstacle != null)
                {
                    UnityEngine.Object.Destroy(
                        obstacle.gameObject);
                }
            }

            _activeObstacles.Clear();
            _fallTweens.Clear();
        }

        private void Spawn(
            ObstacleObjectData data,
            SlashObject slashObject)
        {
            if (data == null || data.Prefab == null)
            {
                return;
            }

            var obstacle =
                UnityEngine.Object.Instantiate(data.Prefab, _parent);
            obstacle.Initialize(data);

            var viewportX = FindViewportX(slashObject);
            var spawnPosition = GetWorldPosition(
                viewportX,
                _config.SpawnViewportY);
            var despawnPosition = GetWorldPosition(
                viewportX,
                _config.DespawnViewportY);
            var rootToRotationCenter =
                obstacle.RotationTarget.position - obstacle.transform.position;

            obstacle.transform.position =
                spawnPosition - rootToRotationCenter;
            _activeObstacles.Add(obstacle);

            var targetY = despawnPosition.y - rootToRotationCenter.y;
            var fallTween = obstacle.transform
                .DOMoveY(targetY, data.FallDuration)
                .SetEase(data.FallEase)
                .SetLink(obstacle.gameObject)
                .OnComplete(() => OnReachedBottom(obstacle));

            if (_config.UseUnscaledTime)
            {
                fallTween.SetUpdate(true);
            }

            _fallTweens[obstacle] = fallTween;
        }

        private float FindViewportX(SlashObject slashObject)
        {
            var minX = _config.ObstacleMinViewportX;
            var maxX = _config.ObstacleMaxViewportX;
            var slashX = _camera.WorldToViewportPoint(
                slashObject.RotationTarget.position).x;
            var bestX = UnityEngine.Random.Range(minX, maxX);
            var bestDistance = Mathf.Abs(bestX - slashX);

            for (var i = 0; i < PositionAttemptCount; i++)
            {
                var candidate = UnityEngine.Random.Range(minX, maxX);
                var distance = GetNearestViewportDistance(
                    candidate,
                    slashX);

                if (distance > bestDistance)
                {
                    bestX = candidate;
                    bestDistance = distance;
                }

                if (distance >= _config.ObstacleMinViewportDistance)
                {
                    return candidate;
                }
            }

            return bestX;
        }

        private float GetNearestViewportDistance(
            float candidate,
            float slashX)
        {
            var nearestDistance = Mathf.Abs(candidate - slashX);

            for (var i = 0; i < _activeObstacles.Count; i++)
            {
                var obstacle = _activeObstacles[i];

                if (obstacle == null)
                {
                    continue;
                }

                var obstacleX = _camera.WorldToViewportPoint(
                    obstacle.RotationTarget.position).x;
                nearestDistance = Mathf.Min(
                    nearestDistance,
                    Mathf.Abs(candidate - obstacleX));
            }

            return nearestDistance;
        }

        private Vector3 GetWorldPosition(float viewportX, float viewportY)
        {
            var distanceFromCamera = Mathf.Abs(
                _positionConfig.ObstacleZ - _camera.transform.position.z);
            var position = _camera.ViewportToWorldPoint(
                new Vector3(viewportX, viewportY, distanceFromCamera));
            position.z = _positionConfig.ObstacleZ;
            return position;
        }

        private void OnReachedBottom(ObstacleObject obstacle)
        {
            _fallTweens.Remove(obstacle);
            _activeObstacles.Remove(obstacle);

            if (obstacle != null)
            {
                UnityEngine.Object.Destroy(obstacle.gameObject);
            }
        }

        private void KillFallTween(ObstacleObject obstacle)
        {
            if (obstacle == null ||
                !_fallTweens.TryGetValue(obstacle, out var tween))
            {
                return;
            }

            if (tween != null && tween.IsActive())
            {
                tween.Kill(false);
            }

            _fallTweens.Remove(obstacle);
        }
    }
}
